﻿using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using FreeIM;

namespace web.Controllers
{
    [Route("ws")]
    public class WebSocketController : Controller
    {
        public string Ip => this.Request.Headers["X-Real-IP"].FirstOrDefault() ?? this.Request.HttpContext.Connection.RemoteIpAddress.ToString();

        static long websocketIdSeed;

        /// <summary>
        /// 获取websocket分区
        /// </summary>
        /// <param name="websocketId">本地标识，若无则不传，接口会返回新的，请保存本地localStoregy重复使用</param>
        /// <returns></returns>
        [HttpPost("pre-connect")]
        public object preConnect([FromForm] long? websocketId)
        {
            if (websocketId == null) websocketId = Interlocked.Increment(ref websocketIdSeed);
            var wsserver = ImHelper.PrevConnectServer(websocketId.Value, this.Ip);
            return new
            {
                code = 0,
                server = wsserver,
                websocketId = websocketId
            };
        }

        [HttpGet("offline")]
        public object offline([FromQuery] long websocketId)
        {
            ImHelper.ForceOffline(websocketId);
            return new
            {
                code = 0
            };
        }

        /// <summary>
        /// 群聊，获取群列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("get-channels")]
        public object getChannels()
        {
            return new
            {
                code = 0,
                channels = ImHelper.GetChanList().Select(a => new { a.chan, a.online })
            };
        }

        /// <summary>
        /// 群聊，绑定消息频道
        /// </summary>
        /// <param name="websocketId">本地标识，若无则不传，接口会返回，请保存本地重复使用</param>
        /// <param name="channel">消息频道</param>
        /// <returns></returns>
        [HttpPost("subscr-channel")]
        public object subscrChannel([FromForm] long websocketId, [FromForm] string channel)
        {
            ImHelper.JoinChan(websocketId, channel);
            return new
            {
                code = 0
            };
        }

        /// <summary>
        /// 群聊，发送频道消息，绑定频道的所有人将收到消息
        /// </summary>
        /// <param name="channel">消息频道</param>
        /// <param name="content">发送内容</param>
        /// <returns></returns>
        [HttpPost("send-channelmsg")]
        public object sendChannelmsg([FromForm] long websocketId, [FromForm] string channel, [FromForm] string message)
        {
            ImHelper.SendChanMessage(0, channel, message);
            return new
            {
                code = 0
            };
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="content">发送内容</param>
        /// <returns></returns>
        [HttpPost("send-broadcast")]
        public object sendChannelmsg([FromForm] string message)
        {
            ImHelper.SendChanMessage(0, null, message);
            return new
            {
                code = 0
            };
        }

        /// <summary>
        /// 单聊
        /// </summary>
        /// <param name="senderWebsocketId">发送者</param>
        /// <param name="receiveWebsocketId">接收者</param>
        /// <param name="message">发送内容</param>
        /// <param name="isReceipt">是否需要回执</param>
        /// <returns></returns>
        [HttpPost("send-msg")]
        public object sendmsg([FromForm] long senderWebsocketId, [FromForm] long receiveWebsocketId, [FromForm] string message, [FromForm] bool isReceipt = false)
        {
            //var loginUser = 发送者;
            //var recieveUser = User.Get(receiveWebsocketId);

            //if (loginUser.好友 != recieveUser) throw new Exception("不是好友");

            ImHelper.SendMessage(senderWebsocketId, new[] { receiveWebsocketId }, message, isReceipt);

            //loginUser.保存记录(message);
            //recieveUser.保存记录(message);

            return new
            {
                code = 0
            };
        }
    }
}