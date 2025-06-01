using FreeRedis;
using Newtonsoft.Json;

/// <summary>
/// im 核心类实现的配置所需
/// </summary>
public class ImClientOptions
{
    /// <summary>
    /// 用于存储数据和发送消息
    /// </summary>
    public RedisClient Redis { get; set; }

    /// <summary>
    /// 负载的服务端
    /// </summary>
    public string[] Servers { get; set; }

    /// <summary>
    /// websocket请求的路径，默认值：/ws
    /// </summary>
    public string PathMatch { get; set; } = "/ws";

    /// <summary>
    /// Json 序列化设置
    /// </summary>
    public JsonSerializerSettings JsonSerializerSettings { get; set; }
}