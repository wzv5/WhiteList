using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using whitelist.Services;

namespace whitelist
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            {
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
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IWhiteListService, DefaultWhiteListService>();
            services.AddSingleton<IMessageService, DefaultMessageService>();
            services.AddSingleton<ILocationService, BaiduLocationService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
