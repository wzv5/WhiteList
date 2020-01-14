using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace whitelist.Services
{
    public interface IMessageService
    {
        public void Send(string msg);
    }

    public class DefaultMessageService : IMessageService
    {
        private readonly ILogger _logger;
        private readonly string _bark;
        private static readonly HttpClient client = new HttpClient();

        public DefaultMessageService(ILogger<DefaultMessageService> logger, IConfiguration config)
        {
            _logger = logger;
            _bark = config["bark"];
            client.Timeout = TimeSpan.FromSeconds(30);
        }
        
        public void Send(string msg)
        {
            _logger.LogInformation("发送消息：{0}", msg);
            if (string.IsNullOrEmpty(_bark))
            {
                return;
            }
            try
            {
                var url = string.Format("{0}/{1}", _bark, msg);
                Task.Run(async () =>
                {
                    await client.GetAsync(url);
                });
            }
            catch (System.Exception)
            {
            }
        }
    }
}
