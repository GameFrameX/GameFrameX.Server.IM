﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FreeIM
{
    class ImServer : ImClient
    {
        protected string _server { get; set; }

        public ImServer(ImServerOptions options) : base(options)
        {
            _server = options.Server;
            _redis.Subscribe($"{_redisPrefix}Server{_server}", RedisSubScribleMessage);
        }

        const int BufferSize = 4096;
        ConcurrentDictionary<long, ConcurrentDictionary<Guid, ImServerClient>> _clients = new ConcurrentDictionary<long, ConcurrentDictionary<Guid, ImServerClient>>();

        class ImServerClient
        {
            public WebSocket socket;
            public long clientId;

            public ImServerClient(WebSocket socket, long clientId)
            {
                this.socket = socket;
                this.clientId = clientId;
            }
        }

        internal async Task Acceptor(HttpContext context, Func<Task> next)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return;
            }

            string token = context.Request.Query["token"];
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var tokenValue = _redis.Get($"{_redisPrefix}Token{token}");
            if (string.IsNullOrEmpty(tokenValue))
            {
                throw new Exception("授权错误：用户需通过 ImHelper.PrevConnectServer 获得包含 token 的连接");
            }

            var data = JsonConvert.DeserializeObject<(long clientId, string clientMetaData)>(tokenValue);

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var cli = new ImServerClient(socket, data.clientId);
            var newid = Guid.NewGuid();

            var wslist = _clients.GetOrAdd(data.clientId, cliid => new ConcurrentDictionary<Guid, ImServerClient>());
            wslist.TryAdd(newid, cli);
            using (var pipe = _redis.StartPipe())
            {
                pipe.HIncrBy($"{_redisPrefix}Online", data.clientId.ToString(), 1);
                pipe.Publish($"evt_{_redisPrefix}Online", tokenValue);
                pipe.EndPipe();
            }

            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);
            try
            {
                while (socket.State == WebSocketState.Open && _clients.ContainsKey(data.clientId))
                {
                    var incoming = await socket.ReceiveAsync(seg, CancellationToken.None);
                    var outgoing = new ArraySegment<byte>(buffer, 0, incoming.Count);
                }

                socket.Abort();
            }
            catch
            {
            }

            wslist.TryRemove(newid, out var oldcli);
            if (wslist.Any() == false)
            {
                _clients.TryRemove(data.clientId, out var oldwslist);
            }

            _redis.Eval($"if redis.call('HINCRBY', KEYS[1], '{data.clientId}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{data.clientId}') end return 1", new[] { $"{_redisPrefix}Online" });
            LeaveChan(data.clientId, GetChanListByClientId(data.clientId));
            _redis.Publish($"evt_{_redisPrefix}Offline", tokenValue);
        }

        void RedisSubScribleMessage(string chan, object msg)
        {
            try
            {
                var msgtxt = msg as string;
                if (msgtxt.StartsWith("__FreeIM__(ForceOffline)"))
                {
                    if (long.TryParse(msgtxt.Substring(24), out var clientId) && _clients.TryRemove(clientId, out var oldclients))
                    {
                        foreach (var oldcli in oldclients)
                        {
                            try
                            {
                                oldcli.Value.socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "disconnect", CancellationToken.None).GetAwaiter().GetResult();
                            }
                            catch
                            {
                            }

                            try
                            {
                                oldcli.Value.socket.Abort();
                            }
                            catch
                            {
                            }

                            try
                            {
                                oldcli.Value.socket.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }

                    return;
                }

                IEnumerable<long> receiveClientIds = null;
                (long senderClientId, List<long>, string content, bool receipt) data;
                if (msgtxt.StartsWith("__FreeIM__(ChanMessage)"))
                {
                    var chanData = JsonConvert.DeserializeObject<(long senderClientId, string receiveChan, string content)>(msgtxt.Substring(23));
                    data.senderClientId = chanData.senderClientId;
                    data.content = chanData.content;
                    data.receipt = false;
                    receiveClientIds = string.IsNullOrEmpty(chanData.receiveChan) ? _clients.Keys : _redis.HKeys($"{_redisPrefix}Chan{chanData.receiveChan}").Select(a => long.TryParse(a, out var tryval) ? tryval : 0).Where(a => a != 0).ToList();
                }
                else
                {
                    data = JsonConvert.DeserializeObject<(long senderClientId, List<long>, string content, bool receipt)>(msgtxt);
                    receiveClientIds = data.Item2;
                    //Console.WriteLine($"收到消息：{data.content}" + (data.receipt ? "【需回执】" : ""));
                }

                var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data.content));
                foreach (var clientId in receiveClientIds)
                {
                    if (_clients.TryGetValue(clientId, out var wslist) == false)
                    {
                        //Console.WriteLine($"websocket{clientId} 离线了，{data.content}" + (data.receipt ? "【需回执】" : ""));
                        if (data.senderClientId != 0 && clientId != data.senderClientId && data.receipt)
                        {
                            SendMessage(clientId, new[] { data.senderClientId }, new
                            {
                                data.content,
                                receipt = "用户不在线"
                            });
                        }

                        continue;
                    }

                    ImServerClient[] sockarray = wslist.Values.ToArray();

                    //如果接收消息人是发送者，并且接收者只有1个以下，则不发送
                    //只有接收者为多端时，才转发消息通知其他端
                    if (clientId == data.senderClientId && sockarray.Length <= 1)
                    {
                        continue;
                    }

                    foreach (var sh in sockarray)
                    {
                        sh.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None)
                          .ContinueWith(async (t, state) =>
                          {
                              if (t.Exception != null)
                              {
                                  var ws = state as WebSocket;
                                  try
                                  {
                                      await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "disconnect", CancellationToken.None);
                                  }
                                  catch
                                  {
                                  }

                                  try
                                  {
                                      ws.Abort();
                                  }
                                  catch
                                  {
                                  }

                                  try
                                  {
                                      ws.Dispose();
                                  }
                                  catch
                                  {
                                  }
                              }
                          }, sh.socket);
                    }

                    if (data.senderClientId != 0 && clientId != data.senderClientId && data.receipt)
                    {
                        SendMessage(clientId, new[] { data.senderClientId }, new
                        {
                            data.content,
                            receipt = "发送成功"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FreeIM.ImServer 订阅方法出错了：{ex.Message}\r\n{ex.StackTrace}");
            }
        }
    }
}