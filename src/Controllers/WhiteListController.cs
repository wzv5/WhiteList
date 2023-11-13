using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using whitelist.Services;

namespace whitelist.Controllers
{
    public static class Ext
    {
        public static void MapWhiteListApi(this RouteGroupBuilder group, IServiceProvider services)
        {
            var controller = ActivatorUtilities.CreateInstance<WhiteListController>(services);
            group.MapGet("/", controller.Get);
            // ASP0016
            group.MapPost("/", (Delegate)controller.Post);

        }
    }

    public class WhiteListController
    {
        private readonly ILogger<WhiteListController> _logger;
        private readonly IWhiteListService _list;
        private readonly string _token;
        private readonly byte[] _html;

        public WhiteListController(ILogger<WhiteListController> logger, IConfiguration config, IWhiteListService list)
        {
            _logger = logger;
            _list = list;
            _token = config["token"];

            // 从内嵌资源中读取 a.html
            var resource = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
            var fi = resource.GetFileInfo("a.html");
            _html = new byte[fi.Length];
            using (var s = fi.CreateReadStream())
            {
                s.ReadExactly(_html);
            }
        }

        public IResult Get()
        {
            return Results.Bytes(_html, "text/html; charset=utf-8");
        }

        public async Task<IResult> Post(HttpContext ctx)
        {
            var form = await ctx.Request.ReadFormAsync();
            if (form.TryGetValue("token", out var token) && token == _token)
            {
                _list.Add(ctx.Connection.RemoteIpAddress);
                return Results.Text("OK");
            }
            else
            {
                _logger.LogWarning("未授权访问：{}", ctx.Connection.RemoteIpAddress);
                return Results.Text("", statusCode: 403);
            }
        }
    }
}
