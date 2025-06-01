// GameFrameX 组织下的以及组织衍生的项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
// 
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
// 
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System;
using System.Collections.Generic;

namespace FreeIM
{
    public class ImSendEventArgs : EventArgs
    {
        /// <summary>
        /// 发送者的客户端id
        /// </summary>
        public long SenderClientId { get; }

        /// <summary>
        /// 接收者的客户端id
        /// </summary>
        public List<long> ReceiveClientId { get; } = new List<long>();

        public string Chan { get; internal set; }

        /// <summary>
        /// imServer 服务器节点
        /// </summary>
        public string Server { get; }

        /// <summary>
        /// 消息
        /// </summary>
        public object Message { get; }

        /// <summary>
        /// 是否回执
        /// </summary>
        public bool Receipt { get; }

        internal ImSendEventArgs(string server, long senderClientId, object message, bool receipt = false)
        {
            this.Server = server;
            this.SenderClientId = senderClientId;
            this.Message = message;
            this.Receipt = receipt;
        }
    }
}