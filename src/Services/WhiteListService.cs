using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace whitelist.Services
{
    public interface IWhiteListService
    {
        public void Add(IPAddress ip);
    }

    public class DefaultWhiteListService : IWhiteListService
    {
        private readonly TimeSpan _timeout = TimeSpan.FromHours(1);
        private readonly Dictionary<IPAddress, DateTime> _list = new Dictionary<IPAddress, DateTime>();
        private List<IPAddress> _lastlist = new List<IPAddress>();
        private readonly Timer _timer;
        private readonly string _listfile;
        private readonly string _nginx;
        private readonly ILogger _logger;
        private readonly IMessageService _msg;
        private readonly ILocationService _location;
        
        public DefaultWhiteListService(ILogger<DefaultWhiteListService> logger, IConfiguration config, IMessageService msg, ILocationService location)
        {
            _listfile = config["file"];
            _nginx = config["nginx"];
            _logger = logger;
            _msg = msg;
            _location = location;
            _timer = new Timer(OnTimer, null, 0, 15000);

            _logger.LogInformation("WhiteListService 已启动");
        }

        public void Add(IPAddress ip)
        {
            lock (this)
            {
                _list[ip] = DateTime.Now + _timeout;
            }
        }

        private void OnTimer(object _)
        {
            lock (this)
            {
                List<IPAddress> _ip = new List<IPAddress>();
                foreach (var (ip, t) in _list)
                {
                    if (DateTime.Now > t)
                    {
                        _ip.Add(ip);
                    }
                }
                foreach (var ip in _ip)
                {
                    _list.Remove(ip);
                }
                var curlist = _list.Keys;
                var newip = curlist.Except(_lastlist).ToArray();
                var delip = _lastlist.Except(curlist).ToArray();
                if (newip.Length > 0 || delip.Length > 0)
                {
                    if (newip.Length > 0)
                    {
                        _logger.LogInformation("新增 IP：\n\t{0}", string.Join("\n\t", newip.AsEnumerable()));
                    }
                    if (delip.Length > 0)
                    {
                        _logger.LogInformation("删除 IP：\n\t{0}", string.Join("\n\t", delip.AsEnumerable()));
                    }
                    _lastlist = curlist.ToList();
                    OnListChanged(curlist);
                    if (newip.Length > 0)
                    {
                        var iplist = new List<string>();
                        foreach (var item in newip)
                        {
                            var addr = _location.GetLocationFromIP(item).Result;
                            if (string.IsNullOrEmpty(addr))
                            {
                                iplist.Add(item.ToString());
                            }
                            else
                            {
                                iplist.Add($"{item}({addr})");
                            }
                        }
                        _msg.Send(string.Join("; ", iplist));
                    }
                }
            }
        }

        private void OnListChanged(ICollection<IPAddress> list)
        {
            if (list.Count > 0)
            {
                _logger.LogInformation("当前列表：\n\t{0}", string.Join("\n\t", list));
            }
            else
            {
                _logger.LogInformation("当前列表：【空】");
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("geo $remote_addr $ip_whitelist {");
            sb.AppendLine("default 0;");
            foreach (var item in list)
            {
                sb.AppendLine(string.Format("{0} 1;", item.ToString()));
            }
            sb.AppendLine("}");
            
            try
            {
                File.WriteAllText(_listfile, sb.ToString());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "写出文件失败：{0}", _listfile);
                return;
            }
            
            var psi = new ProcessStartInfo();
            psi.FileName = _nginx;
            psi.WorkingDirectory = Path.GetDirectoryName(_nginx);
            psi.Arguments = "-t";
            using (var p1 = Process.Start(psi))
            {
                p1.WaitForExit();
                if (p1.ExitCode == 0)
                {
                    _logger.LogInformation("新的配置文件测试通过");
                    psi.Arguments = "-s reload";
                    using (var p2 = Process.Start(psi))
                    {
                        p2.WaitForExit();
                        if (p2.ExitCode == 0)
                        {
                            _logger.LogInformation("已刷新配置");
                        }
                        else
                        {
                            _logger.LogError("刷新配置失败！[{0}]", p2.ExitCode);
                        }
                    }
                }
                else
                {
                    _logger.LogError("新的配置文件测试失败！[{0}]", p1.ExitCode);
                }
            }
        }
    }
}
