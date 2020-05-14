# nginx 动态 IP 白名单

通过动态生成 nginx 配置的方式来实现 IP 白名单。

（没什么技术含量，只是学习和尝试一下 ASP.NET Core）

## 依赖

* .NET Core 3.1
* ASP.NET Core 3.1

## 配置

1. token：密码
2. file：要写出的 nginx 配置文件名
3. nginx：nginx 程序路径
4. bark：bark 消息通知网址，末尾不要带 `/`，留空将不发送消息
5. urls: 配置 asp.net core 的监听网址
6. baidumap_ak: 百度地图 API ak（可选，用于 IP 定位）
7. baidumap_refer: 百度地图 API referer
8. remote_addr_var: nginx 中存储客户端 IP 的变量名，用于兼容 CDN 或其他反代，可选，默认为 `remote_addr`

把配置写入 `src/appsettings.json` 或 `src/appsettings.Development.json`。

示例配置：

``` json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "urls":  "http://localhost:5000",
  "token": "xxx",
  "file": "/etc/nginx/whitelist.conf",
  "nginx":  "/usr/sbin/nginx",
  "bark": "https://xxx.com/token/WhiteList",
  "baidumap_ak": "xxx",
  "baidumap_refer": "https://xxx.com/",
  "remote_addr_var": "my_real_ip"
}

```

## systemd 服务

1. 修改 `mywhitelist.service` 文件中的相关路径
2. 运行 `up.sh` 一键编译和部署
3. 运行 `log.sh` 查看日志

## nginx 配置

``` nginx
server {
    location = /a {
        proxy_redirect      off;
        proxy_pass          http://localhost:5000;
        proxy_http_version  1.1;
        proxy_set_header    Host $host;
        proxy_set_header    X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto $scheme;
    }
}
```

在需要白名单的地方：

``` nginx
include whitelist.conf;

server {
    location = /xxx {
        if ($ip_whitelist != 1) {
            return 403;
        }
    }
}
```

## 把自己的 IP 加入白名单

1. 手动访问 `http://.../a`，在页面中填写 token
2. 或者，直接向 `http://.../a` 发送 POST 请求，参数为 `token=xxx`
3. 成功会看到 `hello`，最多 15 秒后白名单即可生效
4. 成功一次将保持 1 小时，超时后会自动清除，需要再次提交

## 增加安全性

1. 强烈建议使用 https
2. 对于爆破，日志中会输出相关信息，可以配合使用 fail2ban 自动拉黑 IP
