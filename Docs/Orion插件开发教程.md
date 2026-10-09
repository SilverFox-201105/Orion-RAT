# Orion 插件开发指南

> 框架：服务端 **net9.0-windows** ｜ 客户端 **net48** ｜ Orion.Common 双目标
> 本文签名取自当前源码。示例均为可直接抄的最小骨架。

---

## 1. 先搞清楚接口（看完这节再动手）

插件体系对外**可用**的接口只有 **3 个**，其中 2 个跑在服务端、1 个跑在被控端：

| 接口 | 程序集 / 命名空间 | 框架 | 运行在哪 | 干什么 |
|---|---|---|---|---|
| `IServerPlugin` | Orion.Server / `Orion.Server.Plugins` | net9.0-windows | 控端进程 | 扩展控端：加菜单、发命令、收结果 |
| `IUIExtensionPlugin` | Orion.Server / `Orion.Server.Plugins` | net9.0-windows | 控端进程 | 扩展控端**界面**（继承 `IServerPlugin`，多几个 UI 钩子） |
| `IUniversalPlugin` | Orion.Common / `Orion.Common.Plugins` | net48 | 被控端进程 | 在被控机上真正干活 |

### 部署形态：只有两种半的组合

一个插件由 `plugin.json` 的 `server[]` / `client[]` 两个数组决定包含哪几半：

| 形态 | `server[]` | `client[]` | 说明 |
|---|---|---|---|
| 纯服务端 | 有 | `[]` | 只扩展控端（工具、面板、自动化） |
| 纯客户端 | `[]` | 有 | DLL 必须以 **`.Client.dll`** 结尾 |
| 双半 | 有 | 有 | 服务端出 UI 入口，客户端出执行体 |

两边**都为空** → 插件管理器不显示该插件。

---

## 2. 写服务端插件（IServerPlugin）

### 2.1 接口

```csharp
public interface IServerPlugin
{
    string Name { get; }
    Version Version { get; }
    string Description { get; }
    string Type { get; }
    void Initialize(IServerContext context);
    bool AutoLoadToClients => false;   // 有默认实现，可不重写
}
```

契约：`public` + 非抽象 + **公共无参构造函数**。

### 2.2 项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
    <AssemblyName>MyPlugin</AssemblyName>
    <RootNamespace>MyPlugin</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <OutputPath>bin\</OutputPath>
    <GenerateDependencyFile>false</GenerateDependencyFile>
  </PropertyGroup>

  <ItemGroup>
    <!-- ⚠️ 服务端主程序集是 Orion.dll，不是 Orion.Server.dll。
         Orion.exe 是加壳引导壳、没有托管元数据，不能当引用。 -->
    <Reference Include="Orion">
      <HintPath>libs\Orion.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Orion.Common">
      <HintPath>libs\Orion.Common.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <!-- Orion 运行时已自带，别拷进输出目录 -->
    <PackageReference Include="MessagePack" Version="3.1.7">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

> `libs\` 放在项目旁边，从开发者 SDK 的 **`Libs-Contract\`** 把契约程序集 `Orion.dll`、`Orion.Common.dll` 拷进去（仅元数据、不含实现）。
> **不要**用 Orion 运行目录里的真实 `Orion.dll` / `Orion.Common.dll` 替换它们——那份是完整实现，交出去会造成核心源码泄露。

### 2.3 plugin.json

```json
{
  "name": "MyPlugin",
  "version": "1.0.0",
  "description": "我的插件",
  "subfolder": "MyPlugin",
  "server": [ "MyPlugin.dll" ],
  "client": []
}
```

`subfolder` 仅**插件包导入**时生效，决定解压到 `Plugins\` 下哪个子目录（缺省用 `name`）。

> **改了功能务必改 `version`**：服务端按 `PluginId:Version` 缓存分发状态，版本不变不会重新下发给已在线客户端。

### 2.4 最小实现

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Orion.Server.Networking;
using Orion.Server.Plugins;

namespace MyPlugin
{
    public sealed class MyPluginMain : IServerPlugin, IDisposable
    {
        private IServerContext _context;

        public string Name => "我的插件";
        public Version Version => new Version(1, 0, 0, 0);
        public string Description => "最小服务端插件";
        public string Type => "Tool";

        public void Initialize(IServerContext context)
        {
            _context = context;

            // 顶层菜单
            _context.AddClientContextMenuItem("点我", OnClick);
            // 多级菜单（分隔符支持 \ 和 /）
            _context.AddClientContextMenuItemPath(@"我的插件\子菜单", "子项", OnClick);

            _context.Log($"[{Name}] 已加载");
        }

        // clients = 当前在主界面选中的客户端
        private void OnClick(IReadOnlyList<Client> clients)
        {
            _context.Log($"选中 {clients?.Count ?? 0} 个客户端");
        }

        public void Dispose()
        {
            // 若注册了 UniversalPluginResponseHandler 的回调，必须在这里反注册
            _context = null;
        }
    }
}
```

### 2.5 IServerContext 速查

```csharp
Form MainForm { get; }              // 主窗体，跨线程更新 UI 用它 BeginInvoke
OrionServer Server { get; }         // .ConnectedClients → Client[]
void Log(string message);           // 写事件日志
void AddClientContextMenuItem(...)  // 6 个重载，支持分组 / 图标
void AddClientContextMenuItemPath(string path, string text, Action<IReadOnlyList<Client>> onClick);
void ApplyTheme(Action<Form> apply);
void ClearPluginMenuItems();
```

菜单细节：同名菜单项会被**替换**；每次热重载都会重建你在 `Initialize` 里加的菜单，**不要自己缓存菜单对象**。

---

## 3. 写客户端插件（IUniversalPlugin）

### 3.1 接口

```csharp
public interface IUniversalPlugin
{
    string   PluginId { get; }            // 全局唯一
    string   Version { get; }
    string[] SupportedCommands { get; }   // 仅自描述，框架不校验

    void Initialize(object initData);                              // initData: byte[] | null
    PluginResult ExecuteCommand(string command, object parameters); // parameters: byte[] | null
    void Cleanup();
    bool IsComplete { get; }              // true → 本次执行后被卸载
}

public class PluginResult
{
    public bool   Success;
    public string Message;
    public object Data;         // ⚠️ 必须是 byte[]
    public bool   ShouldUnload; // true → 执行后被卸载
    public string NextCommand;
}
```

### 3.2 铁律（违反必炸）

| # | 约束 | 原因 |
|---|---|---|
| 1 | 目标框架 **net48** | 被控端 Orion.Client 是 net48 |
| 2 | `PluginResult.Data` **只能是 `byte[]`** | 客户端 `(byte[])dataProperty.GetValue(result)` 强转 |
| 3 | `ExecuteCommand` 的 `parameters` 运行时是 `byte[]` 或 `null` | 反射调用时传进来的就是消息里的 `Parameters` |
| 4 | 必须有**公共无参构造函数** | `Activator.CreateInstance(type)` |
| 5 | `TypeName` 必须是**完整类型名**（命名空间 + 类名） | `asm.GetType(typeName, throwOnError: true)` |
| 6 | `PluginId` 全局唯一 | 客户端按 ID 存 `ConcurrentDictionary`，重复会覆盖 |
| 7 | `Initialize` 必须快速返回 | 跑在被控端消息线程上，卡住会阻塞心跳 |

### 3.3 项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Platforms>AnyCPU</Platforms>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <OutputPath>bin\</OutputPath>
    <!-- 必须以 .Client 结尾 → 产出 MyPlugin.Client.dll -->
    <AssemblyName>MyPlugin.Client</AssemblyName>
    <RootNamespace>MyPlugin.ClientModule</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- 只引用 net48 版的 Orion.Common -->
    <Reference Include="Orion.Common">
      <HintPath>libs\net48\Orion.Common.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

> 用 `System.IO.Compression` 时要显式加 `<Reference Include="System.IO.Compression" />` + `Microsoft.NETFramework.ReferenceAssemblies` 包。

### 3.4 最小实现

```csharp
using System;
using System.Text;
using Orion.Common.Plugins;

namespace MyPlugin.ClientModule
{
    // 服务端要用 TypeName = "MyPlugin.ClientModule.TaskPlugin"
    public sealed class TaskPlugin : IUniversalPlugin
    {
        // ⚠️ 这三个属性会在控端（net9 进程）里被反射读取，必须是零依赖常量
        public string   PluginId => "MyPlugin_Task";
        public string   Version  => "1.0.0";
        public string[] SupportedCommands => new[] { "run" };

        public bool IsComplete => false;

        public void Initialize(object initData)
        {
            // initData: byte[] | null
        }

        public PluginResult ExecuteCommand(string command, object parameters)
        {
            try
            {
                var args = parameters as byte[];
                var text = args != null && args.Length > 0 ? Encoding.UTF8.GetString(args) : "";

                return command == "run"
                    ? Ok("done: " + text)
                    : Fail("不支持的命令: " + command);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);   // 抛异常会导致插件被卸载，务必兜住
            }
        }

        public void Cleanup() { }

        private static PluginResult Ok(string s) => new PluginResult
        {
            Success = true, Message = "OK",
            Data = Encoding.UTF8.GetBytes(s ?? ""),   // ⚠️ byte[]
            ShouldUnload = false, NextCommand = null
        };

        private static PluginResult Fail(string m) => new PluginResult
        {
            Success = false, Message = m,
            Data = Encoding.UTF8.GetBytes(""),        // ⚠️ byte[]
            ShouldUnload = false, NextCommand = null
        };
    }
}
```

### 3.5 net48 依赖约束（最容易踩）

你的 net48 DLL 会被**两个进程**加载：被控端（真正执行）+ 控端（catalog 只为读 `PluginId`/`Version`）。
在 net9 进程里加载 net48 程序集时，只要类型引用了 net9 不存在的程序集就会抛 `ReflectionTypeLoadException`，**插件连登记都登记不上**。

- ✅ 只引用 `Orion.Common.dll` + mscorlib / System / System.Core
- ✅ `PluginId` / `Version` / `SupportedCommands` 直接返回常量字符串
- ✅ 需要 Win32 API 用 P/Invoke；需要 WMI 就 `Process` 起 `cmd /c wmic ...` 读输出
- ❌ 不要引用 `System.Windows.Forms` / `System.Management` / NAudio / SharpDX

---

## 4. 双半插件：服务端联动客户端

**怎么组合**：复制 `ServerPlugin`（或 `UiExtensionPlugin`）当服务端一半 + `ClientPlugin` 当客户端一半，在 `plugin.json` 里同时填 `server[]` 和 `client[]`，就是一个双半插件。服务端怎么把命令发到客户端，有下列三种方式，任选其一。

### 4.1 方式 A —— `plugin.json` 的 `client` 数组（推荐）

`ClientPluginCatalog` 会**递归**扫描 `Plugins\` 下所有层级的 `*.Client.dll`，并在客户端上线时自动分发（按 `PluginId:Version` 去重）。

服务端直接发命令即可，不用管下发：

```csharp
PushSender.ExecuteUniversalCommand(client, "MyPlugin_Task", "run", Encoding.UTF8.GetBytes("hi"));
```

### 4.2 方式 B —— `AutoLoadToClients`

```csharp
public bool AutoLoadToClients => true;
```
服务端把客户端模块作为**内嵌资源**打包，客户端上线时自动下发：

```xml
<EmbeddedResource Include="..\MyPlugin.Client\bin\MyPlugin.Client.dll"
                  LogicalName="MyPlugin.MyPlugin.ClientModule.dll" />
```

> ⚠️ 方式 B 生成的 PluginId 是 `{类名}_{随机GUID}`，**无法精确寻址**。只适合"加载即执行"的一次性任务。要后续交互请用方式 A。

### 4.3 手动下发 + 收响应

```csharp
UniversalPluginResponseHandler.RegisterType("MyPlugin", OnResponse);   // 长期回调
// 类型 = PluginId 中第一个 '_' 之前的子串："MyPlugin_Task" → "MyPlugin"

PushSender.LoadUniversalPlugin(client, "MyPlugin_Task", clientDllBytes,
                               initData: null, typeName: "MyPlugin.ClientModule.TaskPlugin",
                               methodName: "Initialize");            // 幂等，同 ID 只注册一次
PushSender.ExecuteUniversalCommand(client, "MyPlugin_Task", "run", null);

private void OnResponse(PluginResponse r)
{
    if (r.Command == "load") { /* 加载结果 */ return; }
    if (!r.Success) { /* 失败 */ return; }
    var text = r.GetText();   // = Encoding.UTF8.GetString(Data)
}
```

### 4.4 ⚠️ 读取客户端 DLL 字节：别用 `Assembly.Location`

插件是**字节加载**的（`Assembly.Load(File.ReadAllBytes(path))`），**`Assembly.Location` 为空**。任何 `Path.GetDirectoryName(GetExecutingAssembly().Location)` 的定位方式都会失败。

正确写法：

```csharp
private static byte[] LoadClientModule(string dllName)
{
    var candidates = new List<string>();
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;

    if (!string.IsNullOrEmpty(baseDir))
    {
        var pluginsRoot = Path.Combine(baseDir, "Plugins");
        if (Directory.Exists(pluginsRoot))
        {
            try
            {
                foreach (var f in Directory.GetFiles(pluginsRoot, dllName, SearchOption.AllDirectories))
                    candidates.Add(f);
            }
            catch { }
        }
        candidates.Add(Path.Combine(baseDir, dllName));
    }

    foreach (var p in candidates)
    {
        try { if (File.Exists(p)) return File.ReadAllBytes(p); } catch { }
    }
    throw new FileNotFoundException($"找不到 {dllName}（应在 Plugins 目录下）", dllName);
}
```

> 若已在 `plugin.json` 声明了 `client`，则**无需自己读字节**——catalog 会自动分发，直接调用 `ExecuteUniversalCommand` 即可。

### 4.5 一定要 Dispose 反注册

`UniversalPluginResponseHandler` 的回调表是**静态**的，热重载不会清理 → 重复触发。

```csharp
public void Dispose()
{
    UniversalPluginResponseHandler.UnregisterType("MyPlugin", OnResponse);
}
```
`PluginManager.ReloadPlugins` 会对所有 `IDisposable` 插件调 `Dispose()`。

---

## 5. UI 扩展（可选）

实现 `IUIExtensionPlugin`（继承 `IServerPlugin`），即可往控端主界面注入界面元素：

```csharp
TabPage[]           CreateCustomTabs();      // 加页签
ToolStripItem[]     CreateToolbarItems();    // 加工具栏按钮
ToolStripMenuItem[] CreateMenuItems();       // 加到「工具」菜单下
void CustomizeForm(Form form);
void CustomizeControl(Control control);      // ⚠️ 对主窗体每个控件递归调用，逻辑要极轻量
Form CreateCustomMainForm();
bool ShouldReplaceMainForm { get; }          // 保持 false
int  UIPriority { get; }                     // 升序，越小越靠前
```

铁律：**所有返回值必须非 null**，不需要就返回 `Array.Empty<T>()`。

插件自己 `new` 的窗体要手动套深色：

```csharp
using Orion.Server.Forms.DarkMode;
DarkModeManager.ApplyDarkMode(form);
```

嵌进主界面的 `TabPage` / `ToolStripItem` **不会**被自动染色，请手写配色
（背景 `Color.FromArgb(45,45,48)`、控件底 `Color.FromArgb(37,37,40)`、前景 `White`）。

---

## 6. 目录、加载与热重载

### 6.1 目录结构

```
<Orion.exe 所在目录>\
└── Plugins\
    └── MyPlugin\              ← 每个插件一个子目录
        ├── plugin.json        ← 必需，没有它整个目录被跳过
        ├── MyPlugin.dll
        ├── MyPlugin.Client.dll
        └── Newtonsoft.Json.dll
```

### 6.2 加载要点

- 服务端：遍历每个子目录读 `plugin.json` → `Assembly.Load(字节)` → 找实现 `IServerPlugin` 的类型 → `Initialize`。文件名以 `.Client.dll` 结尾的会被跳过（防止误加载进服务端）。
- 客户端 catalog：递归扫描 `*.Client.dll`（深度上限 8 层），注册后发现 `PluginsChanged` → 自动分发给在线客户端。
- **以字节加载，不锁文件**，所以插件 DLL 可以在 Orion 运行时被覆盖。
- 第三方依赖放自己插件目录即可（`AssemblyResolve` 会遍历插件子目录找）；不同插件的同名依赖会互相干扰，**依赖名请加前缀**。

### 6.3 热重载

`FileSystemWatcher`（含子目录）捕获变更 → **去抖约 1.5 秒** → `ReloadPlugins()`：Dispose 旧插件 → 清 UI 扩展 → 清菜单 → 重新加载。

**调试技巧**：编译后把 DLL 拷进 `Plugins\<你的插件>\`，1 秒后自动重载，无需重启。

> 旧程序集**不会**从 AppDomain 卸载（.NET 不支持），频繁重载内存会持续增长，属正常现象。

### 6.4 资源文件（.sys / .ico 等）

二进制资源随插件目录分发，运行时按 `Plugins\<subfolder>\<file>` 定位：

```csharp
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
string direct  = Path.Combine(baseDir, "Plugins", "MyPlugin", "extra.sys");
// 兜底：Directory.GetFiles(pluginsRoot, fileName, SearchOption.AllDirectories)
```

在 csproj 里把资源拷进输出：

```xml
<Target Name="CopyExtraFiles" AfterTargets="Build">
  <Copy SourceFiles="assets\extra.sys" DestinationFolder="$(OutDir)" />
</Target>
```

> 注意：不能用 `Assembly.Location` 拼路径（见 [4.4](#44-️读取客户端-dll-字节别用-assemblylocation)）。

---

## 7. 打包成 .Orion

`.Orion` 就是改了扩展名的 **ZIP**。里面须含 `plugin.json`，**允许子目录**（依赖、架构目录等），结构会被原样保留。

**7-Zip（推荐）**
```bat
pushd "C:\dev\MyPlugin\publish"
"C:\Program Files\7-Zip\7z.exe" a -tzip -mx=9 "..\MyPlugin.Orion" * -r
popd
```

**PowerShell**
```powershell
Compress-Archive -Path "C:\dev\MyPlugin\publish\*" -DestinationPath "MyPlugin.Orion" -Force
```

> 必须 `cd` 进目录再压缩内容，**不要**在外面压缩整个文件夹（会多包一层目录）。
> 导入器带 **Zip Slip 防护**：含 `../` 或绝对路径的条目会被拒绝。

**安装**：插件管理器 → 导入（可多选），或**直接把 `.Orion` 拖到插件列表上**。

---

## 8. 排错速查

| 现象 | 原因 | 解决 |
|---|---|---|
| 插件管理器**看不到**插件 | `plugin.json` 缺失，或 `server`/`client` 都为空 | 检查文件名与字段拼写 |
| 显示了但**没生效** | 类不是 `public` / 无公共无参构造 / 没实现 `IServerPlugin` | 检查可见性与构造函数 |
| 日志 "Plugin init failed" | `Initialize` 抛异常 | UI 操作要用 `BeginInvoke` |
| 客户端插件 ReflectionTypeLoadException | net48 DLL 引用了 net9 缺失程序集 | 见 [3.5](#35-net48-依赖约束最容易踩) |
| "Duplicate client plugin id" | 两个 `.Client.dll` 的 `PluginId` 相同 | 保证唯一 |
| 加载成功但命令无响应 | `TypeName` 不是完整类型名，或两端 `PluginId` 不一致 | 核对命名空间+类名、ID 拼写 |
| 命令返回 InvalidCastException | `PluginResult.Data` 不是 `byte[]` | 见 [3.2](#32-铁律违反必炸) |
| 改了代码没重新分发 | 缓存 key 是 `PluginId:Version` | 改 `version` |
| 找不到客户端 DLL | 用了 `Assembly.Location` 定位 | 见 [4.4](#44-️读取客户端-dll-字节别用-assemblylocation) |

---

## 9. API 速查

```csharp
// PushSender（Orion.Server.Networking）
static void LoadUniversalPlugin(Client c, string pluginId, byte[] bytes,
                                byte[] initData, string typeName, string methodName);
static void ExecuteUniversalCommand(Client c, string pluginId, string command, byte[] parameters);

// UniversalPluginResponseHandler（Orion.Server.Messages）
static void Register(string pluginId, Action<PluginResponse> cb);        // 一次性
static void RegisterType(string pluginType, Action<PluginResponse> cb);  // 长期
static void Unregister / UnregisterType(...);
static event Action<PluginResponse> ResponseReceived;                    // 全局

// PluginResponse
Client Client; string PluginId; string Command; bool Success;
string Message; byte[] Data; bool ShouldUnload; string NextCommand;
string GetText();   // Encoding.UTF8.GetString(Data)
```

---

## 10. 已知限制

1. **程序集无法卸载** —— .NET 不支持卸载单个 Assembly，热重载只清引用+重新加载字节，内存持续增长。
2. **`AutoLoadToClients` 的 PluginId 带随机 GUID** —— 无法精确寻址，只适合一次性任务。
3. **回调表是静态的** —— 必须自己 `IDisposable` 反注册，否则重复触发。
4. **`CustomizeControl` 递归调用** —— 主窗体几百个控件都会过一遍，逻辑必须极轻量。

---

## 附录 A 模板工程

```
Docs/PluginTemplates/
├── ServerPlugin/        net9 服务端插件（最小可用）
├── UiExtensionPlugin/   net9 UI 扩展（Tab / 工具栏 / 菜单）
├── ClientPlugin/        net48 客户端插件（**.Client.dll**）
└── build-package.bat    一键打包成 .Orion
```

使用步骤：

1. 复制模板目录
2. 从开发者 SDK 的 **`Libs-Contract\`** 拷贝契约程序集：`Orion.dll`、`Orion.Common.dll` 到 `libs\`；客户端另需 `libs\net48\Orion.Common.dll`（均来自 `Libs-Contract\net48\`）。**切勿使用运行目录里的真实 DLL。**
3. 改命名空间、类名、`plugin.json`
4. `build-package.bat Server`（或 `Ui`）
5. 插件管理器 → 导入

**做双半插件**：服务端用 `ServerPlugin` 并填好 `client[]`，打包时把客户端项目作为第二个参数传进去：

```
build-package.bat Server ..\ClientPlugin\MyClientPlugin.Client.csproj
```

---

## 发布前检查清单

- [ ] `plugin.json` 在插件目录根，字段拼写正确
- [ ] `server` DLL 是 **net9.0-windows**；`client` DLL 是 **net48** 且以 **`.Client.dll`** 结尾
- [ ] 两端类都是 `public` + 非抽象 + 有公共无参构造函数
- [ ] 客户端 `PluginId` 全局唯一，`TypeName` 是完整类型名
- [ ] `PluginResult.Data` 一律 `byte[]`
- [ ] 客户端**没有**引用 `System.Windows.Forms` / `System.Management`
- [ ] 引用的是 `libs\Orion.dll`（不是 `Orion.Server.dll`）
- [ ] 定位插件目录文件用的是 `BaseDirectory\Plugins`，**没有**用 `Assembly.Location`
- [ ] `RegisterType` 的类型名 = `PluginId` 第一个 `_` 之前的子串
- [ ] 注册过回调的插件实现了 `IDisposable` 并在 `Dispose` 反注册
- [ ] 更新功能后改了 `version`

---

## 11. 提交上架官方插件市场

开发完成的插件可以提交审核，通过后在官方插件市场上架，由你自行设定免费或自定义价格。

1. **注册开发者** —— 通过 Telegram（`t.me/Silver_Fox_201105`）联系主开发者，登记为插件开发者。
2. **开发并自测** —— 按本文档第 1–10 节完成插件，过一遍上方「发布前检查清单」。
3. **提交源码** —— 将插件**源码**（含 `plugin.json` 与必要说明）发给主开发者审核。
4. **审核** —— 主开发者进行源码审计与行为扫描，确保无恶意代码、无未授权后门。
5. **上架与定价** —— 审核通过后，打包为 `.Orion` 上架官方市场；你可设定**免费**或**自定义价格**，收益归开发者所有。

> ⚠️ 审核聚焦安全与质量：提交前请确保插件不包含任何未声明的网络回传、持久化或破坏性行为；通过审核的插件才会进入官方市场供他人下载。
