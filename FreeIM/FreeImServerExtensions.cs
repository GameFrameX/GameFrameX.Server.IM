#if ns20

// GameFrameX 组织下的以及组织衍生的项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
// 
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
// 
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Microsoft.AspNetCore.Builder;

namespace FreeIM
{
    public static class FreeImServerExtensions
    {
        static bool isUseWebSockets = false;

        /// <summary>
        /// 启用 ImServer 服务端
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFreeImServer(this IApplicationBuilder app, ImServerOptions options)
        {
            app.Map(options.PathMatch, appcur =>
            {
                var imserv = new ImServer(options);
                if (isUseWebSockets == false)
                {
                    isUseWebSockets = true;
                    appcur.UseWebSockets();
                }

                appcur.Use((ctx, next) =>
                               imserv.Acceptor(ctx, next));
            });
            return app;
        }
    }
}
#endif