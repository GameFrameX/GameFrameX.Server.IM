﻿using System;
using System.Collections.Generic;

namespace FreeIM
{
    /// <summary>
    /// im 核心类 ImClient 实现的静态代理类
    /// </summary>
    public static class ImHelper
    {
        static ImClient _instance;

        public static ImClient Instance
        {
            get { return _instance ?? throw new Exception("使用前请初始化 ImHelper.Initialization(...);"); }
        }

        /// <summary>
        /// 初始化 ImHelper
        /// </summary>
        /// <param name="options"></param>
        public static void Initialization(ImClientOptions options)
        {
            _instance = new ImClient(options);
        }

        /// <summary>
        /// ImServer 连接前的负载、授权，返回 ws 目标地址，使用该地址连接 websocket 服务端
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="clientMetaData">客户端相关信息，比如ip</param>
        /// <returns>websocket 地址：ws://xxxx/ws?token=xxx</returns>
        public static string PrevConnectServer(long clientId, string clientMetaData)
        {
            return Instance.PrevConnectServer(clientId, clientMetaData);
        }

        /// <summary>
        /// 向指定的多个客户端id发送消息
        /// </summary>
        /// <param name="senderClientId">发送者的客户端id</param>
        /// <param name="receiveClientId">接收者的客户端id</param>
        /// <param name="message">消息</param>
        /// <param name="receipt">是否回执</param>
        public static void SendMessage(long senderClientId, IEnumerable<long> receiveClientId, object message, bool receipt = false)
        {
            Instance.SendMessage(senderClientId, receiveClientId, message, receipt);
        }

        /// <summary>
        /// 获取所在线客户端id
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<long> GetClientListByOnline()
        {
            return Instance.GetClientListByOnline();
        }

        /// <summary>
        /// 判断客户端是否在线
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static bool HasOnline(long clientId)
        {
            return Instance.HasOnline(clientId);
        }

        /// <summary>
        /// 判断客户端是否在线（多个）
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public static bool[] HasOnline(IEnumerable<long> clientIds)
        {
            return Instance.HasOnline(clientIds);
        }

        /// <summary>
        /// 强制下线
        /// </summary>
        /// <param name="clientId"></param>
        public static void ForceOffline(long clientId)
        {
            Instance.ForceOffline(clientId);
        }

        /// <summary>
        /// 事件订阅
        /// </summary>
        /// <param name="online">上线</param>
        /// <param name="offline">下线</param>
        public static void EventBus(Action<(long clientId, string clientMetaData)> online, Action<(long clientId, string clientMetaData)> offline)
        {
            Instance.EventBus(online, offline);
        }

        #region 群聊频道，每次上线都必须重新加入

        /// <summary>
        /// 加入群聊频道，每次上线都必须重新加入
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="chans">群聊频道名</param>
        public static void JoinChan(long clientId, params string[] chans)
        {
            Instance.JoinChan(clientId, chans);
        }

        /// <summary>
        /// 离开群聊频道
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="chans">群聊频道名</param>
        public static void LeaveChan(long clientId, params string[] chans)
        {
            Instance.LeaveChan(clientId, chans);
        }

        /// <summary>
        /// 离开群聊频道
        /// </summary>
        /// <param name="chan">群聊频道名</param>
        /// <param name="clientIds">客户端id</param>
        public static void LeaveChan(string chan, params long[] clientIds)
        {
            Instance.LeaveChan(chan, clientIds);
        }

        /// <summary>
        /// 获取群聊频道所有客户端id（测试）
        /// </summary>
        /// <param name="chan">群聊频道名</param>
        /// <returns></returns>
        public static long[] GetChanClientList(string chan)
        {
            return Instance.GetChanClientList(chan);
        }

        /// <summary>
        /// 清理群聊频道的离线客户端（测试）
        /// </summary>
        /// <param name="chan">群聊频道名</param>
        public static void ClearChanClient(string chan)
        {
            Instance.ClearChanClient(chan);
        }

        /// <summary>
        /// 获取所有群聊频道和在线人数
        /// </summary>
        /// <returns>频道名和在线人数</returns>
        public static IEnumerable<(string chan, long online)> GetChanList()
        {
            return Instance.GetChanList();
        }

        /// <summary>
        /// 获取用户参与的所有群聊频道
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <returns></returns>
        public static string[] GetChanListByClientId(long clientId)
        {
            return Instance.GetChanListByClientId(clientId);
        }

        /// <summary>
        /// 获取群聊频道的在线人数
        /// </summary>
        /// <param name="chan">群聊频道名</param>
        /// <returns>在线人数</returns>
        public static long GetChanOnline(string chan)
        {
            return Instance.GetChanOnline(chan);
        }

        /// <summary>
        /// 发送群聊消息，所有在线的用户将收到消息
        /// </summary>
        /// <param name="senderClientId">发送者的客户端id</param>
        /// <param name="chan">群聊频道名</param>
        /// <param name="message">消息</param>
        public static void SendChanMessage(long senderClientId, string chan, object message)
        {
            Instance.SendChanMessage(senderClientId, chan, message);
        }

        /// <summary>
        /// 发送广播消息
        /// </summary>
        /// <param name="message">消息</param>
        public static void SendBroadcastMessage(object message)
        {
            Instance.SendBroadcastMessage(message);
        }

        #endregion
    }
}