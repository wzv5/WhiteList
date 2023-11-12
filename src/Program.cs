using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using whitelist.Controllers;
using whitelist.Services;

namespace whitelist
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            // 检查配置
            var configuration = builder.Configuration;
            var token = configuration["token"];
            var file = configuration["file"];
            var nginx = configuration["nginx"];
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(file) || string.IsNullOrEmpty(nginx))
            {
                throw new ArgumentNullException();
            }
            if (!File.Exists(nginx))
            {
                throw new FileNotFoundException(nginx);
            }

            // 配置服务
            var services = builder.Services;
            services.AddSingleton<IWhiteListService, DefaultWhiteListService>();
            services.AddSingleton<IMessageService, DefaultMessageService>();
            services.AddSingleton<ILocationService, BaiduLocationService>();
            var app = builder.Build();

            // 配置路由
            app.MapGroup("/a").MapWhiteListApi(app.Services);

            app.Run();
        }
    }
}
