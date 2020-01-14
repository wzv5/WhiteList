using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using whitelist.Services;

namespace whitelist.Controllers
{
    [ApiController]
    [Route("a")]
    public class WhiteListController : ControllerBase
    {
        private readonly ILogger<WhiteListController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IWhiteListService _list;
        private readonly string _token;

        public WhiteListController(ILogger<WhiteListController> logger, IConfiguration config, IWebHostEnvironment env, IWhiteListService list)
        {
            _logger = logger;
            _env = env;
            _list = list;
            _token = config["token"];
        }

        [HttpGet]
        public IActionResult Get()
        {
            return PhysicalFile(Path.Combine(_env.WebRootPath, "a.html"), "text/html;charset=utf-8");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var form = await Request.ReadFormAsync();
            if (form.TryGetValue("token", out var token) && token == _token)
            {
                _list.Add(HttpContext.Connection.RemoteIpAddress);
                return Ok("hello");
            }
            else
            {
                _logger.LogWarning("未授权访问：{0}", HttpContext.Connection.RemoteIpAddress);
                return StatusCode(403, "");
            }
        }
    }
}
