using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace whitelist.Services
{
    public interface ILocationService
    {
        public Task<string> GetLocationFromIP(IPAddress ip);
    }

    public class BaiduLocationService : ILocationService
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly string _baidu_ak;
        private readonly string _baidu_refer;
        private static readonly HttpClient client = new HttpClient();

        public BaiduLocationService(ILogger<BaiduLocationService> logger, IConfiguration config)
        {
            _logger = logger;
            _baidu_ak = config["baidumap_ak"];
            _baidu_refer = config["baidumap_refer"];

            // 如果缺少参数，则跳过初始化
            if (string.IsNullOrEmpty(_baidu_ak) || string.IsNullOrEmpty(_baidu_refer))
            {
                _baidu_ak = null;
                _baidu_refer = null;
                return;
            }

            client.Timeout = TimeSpan.FromSeconds(30);
            client.BaseAddress = new Uri("https://api.map.baidu.com/location/ip");
            client.DefaultRequestHeaders.Referrer = new Uri(_baidu_refer);

            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 50
            });
        }
        
        public async Task<string> GetLocationFromIP(IPAddress ip)
        {
            if (_cache.TryGetValue(ip, out String addr))
            {
                _logger.LogDebug($"从缓存获取 {ip} 的位置为 {addr}");
                return addr;
            }
            if (ip.AddressFamily != AddressFamily.InterNetwork || string.IsNullOrEmpty(_baidu_ak))
            {
                return "";
            }
            try
            {
                var resp = await client.GetAsync($"?ak={_baidu_ak}&ip={ip}");
                resp.EnsureSuccessStatusCode();
                var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                var status = doc.RootElement.GetProperty("status").GetInt32();
                if (status == 0)
                {
                    var address = doc.RootElement.GetProperty("content").GetProperty("address").GetString();
                    _logger.LogDebug($"联网获取 {ip} 的位置为 {address}");
                    _cache.Set(ip, address, new MemoryCacheEntryOptions() {
                        Size = 1,
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return address;
                }
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"联网获取 {ip} 的位置失败");
            }
            return "";
        }
    }
}
