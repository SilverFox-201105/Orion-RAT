# Orion

Orion 是一个 Windows 平台的远程管理与安全评估工具，由服务端（控端）、客户端（被控端）
与公共库三个项目组成：

| 项目 | 说明 |
| --- | --- |
| `Orion.Server` | 控端（WinForms，net9.0-windows），含客户端构建器与插件管理 |
| `Orion.Client` | 被控端（WinForms，net48），由控端的构建器生成 |
| `Orion.Common` | 两端共享的消息契约、网络与加密基础设施 |

功能范围：远程桌面、文件管理、进程/服务/注册表/启动项管理、远程 Shell 与脚本执行、
系统信息、音视频采集、插件扩展等。

## 合法使用声明

本工具**仅可用于**你拥有所有权、或已取得所有权人书面授权的系统，用于安全评估、
渗透测试、红队演练与教学研究。严禁用于任何未取得授权的系统。
使用者对自身行为独立承担全部法律责任。

## 构建

环境：Visual Studio 2022（.NET 桌面开发工作负载）+ .NET 9 SDK。

```
dotnet build Orion.sln -c Release
```

构建被控端请使用控端内置的客户端构建器（Builder），它会读取 `client.bin`
并写入连接配置。`client.bin` 需先由 `Orion.Client` 的 Release 产物生成并提供到控端目录。

## 许可

源码采用 **Orion Source-Available License v1.0**（见 `LICENSE.md`）：

- 允许：阅读、学习、审计，以及非商业目的的本地编译运行；
- 禁止：任何商业使用；二次开发与再分发（含 fork、镜像、修改版）。

二进制程序的最终用户授权另见 `Licenses/EULA.md`。
部分代码派生自 Quasar RAT（MIT License, Copyright (c) 2025 MaxXor）。
