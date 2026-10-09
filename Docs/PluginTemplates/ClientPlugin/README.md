# 客户端插件模板（net48）

## 硬性约束

| 项 | 要求 |
|---|---|
| 目标框架 | **net48**（与 Orion.Client 一致） |
| 程序集名 | 必须以 **`.Client`** 结尾 → 产出 `MyClientPlugin.Client.dll` |
| 接口 | 实现 `Orion.Common.Plugins.IUniversalPlugin` |
| 可见性 | 类必须 `public`、非抽象、有**公共无参构造函数** |
| `TypeName` | 服务端下发时必须写**完整类型名**：`MyClientPlugin.ClientModule.SysInfoPlugin` |
| `PluginResult.Data` | **只能是 `byte[]`**（客户端 `CommandHandler` 会强转，其它类型直接 `InvalidCastException`） |
| `parameters` | `ExecuteCommand` 第二参数运行时是 `byte[]` 或 `null` |

## ⚠️ 依赖约束（最容易踩的坑）

这个 DLL 会在**两个进程**里被加载：

1. **被控端**（net48）—— 真正执行你的代码
2. **服务端**（net9）—— `ClientPluginCatalog` 用 `Assembly.Load` 只为读 `PluginId` / `Version`

net9 加载 net48 程序集时，只要类型引用了 net9 中不存在的程序集（`System.Windows.Forms`、`System.Drawing`、`System.Management`…），就会抛 `ReflectionTypeLoadException`，你的插件**连登记都登记不上**，服务端日志会打：

```
Client plugin load error (MyClientPlugin.Client.dll): ...
```

**所以：**

- ✅ 只引用 `Orion.Common.dll` + `mscorlib` / `System` / `System.Core`
- ✅ `PluginId` / `Version` / `SupportedCommands` 三个属性直接返回**常量字符串**
- ✅ 需要 Win32 API → `DllImport`
- ✅ 需要 WMI → `System.Diagnostics.Process` 起 `cmd /c wmic ...` 读标准输出
- ❌ 不要引用 `System.Windows.Forms`（要弹窗就 P/Invoke `MessageBoxW`）
- ❌ 不要引用 NAudio / SharpDX / AForge / Newtonsoft.Json（它们不会跟着你的 DLL 一起下发）

真有重依赖时用 **Costura.Fody** 合并成单 DLL。

## 生命周期

```
服务端 PushSender.LoadUniversalPlugin(id, bytes, initData, typeName, "Initialize")
   └─▶ 客户端 Assembly.Load(bytes)
       → asm.GetType(typeName, throwOnError: true)
       → Activator.CreateInstance()
       → Initialize(initData)               ← initData: byte[] | null
       → UniversalPluginDispatcher.RegisterPlugin(id, plugin)

服务端 PushSender.ExecuteUniversalCommand(id, "sysinfo", parameters)
   └─▶ 客户端反射调 ExecuteCommand("sysinfo", parameters)
       → 读 Success/Message/Data/ShouldUnload/NextCommand
       → 若 ShouldUnload || IsComplete → Cleanup() 并卸载
       → 回传 DoUniversalPluginResponse
```

## 命令

| 命令 | 参数 | 返回 |
|---|---|---|
| `sysinfo` | 无 | 机器名、用户、OS、CPU 数、系统目录、CLR 版本 |
| `drives` | 无 | 逻辑驱动器列表 |
| `env` | 无 | 全部环境变量 |
| `echo` | UTF-8 文本 | 原样回显，用来验证链路 |

## 修改版本后

服务端按 `PluginId:Version` 缓存分发状态。**改了代码必须同步改 `plugin.json` 的 `version`**，否则已在线的客户端不会收到新版本。
