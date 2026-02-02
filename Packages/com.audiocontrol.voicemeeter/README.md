# VoiceMeeter Audio Controller

Unity 音频输出控制插件，通过 VoiceMeeter 实现 A/B 模式切换。

## 功能

- **A/B 模式切换**：A 模式（仅耳机）/ B 模式（耳机+音箱）
- **自动重连**：VoiceMeeter 断开后自动重连
- **日志系统**：记录模式切换历史，便于调试
- **事件驱动**：提供模式切换、连接状态变化等事件

## 安装

### 方式一：Package Manager (Git URL)

1. 打开 Unity → Window → Package Manager
2. 点击 `+` → Add package from git URL
3. 输入：`https://github.com/你的仓库/com.audiocontrol.voicemeeter.git`

### 方式二：本地安装

1. 将 `com.audiocontrol.voicemeeter` 文件夹复制到项目的 `Packages/` 目录

## 前置要求

1. 安装 [VoiceMeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm)
2. 安装 [VB-Audio Virtual Cable](https://vb-audio.com/Cable/)
3. 配置 Windows 默认输出设备为 `VoiceMeeter Input (VB-Audio VoiceMeeter VAIO)`
4. 配置 VoiceMeeter A1/A2 输出设备

## 快速开始

### 1. 添加控制器

```csharp
// 自动创建单例
var controller = AudioOutputController.Instance;
```

或在场景中创建 GameObject 并添加 `AudioOutputController` 组件。

### 2. 切换模式

```csharp
// 切换到 A 模式（仅耳机）
controller.SetModeA();

// 切换到 B 模式（耳机+音箱）
controller.SetModeB();

// 切换模式
controller.ToggleMode();
```

### 3. 监听事件

```csharp
controller.OnModeChanged += (mode) => {
    Debug.Log($"Mode changed to: {mode}");
};

controller.OnConnectionChanged += (connected) => {
    Debug.Log($"Connected: {connected}");
};

controller.OnReconnecting += () => {
    Debug.Log("Reconnecting...");
};

controller.OnReconnected += () => {
    Debug.Log("Reconnected!");
};
```

## Inspector 配置

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Strip Index | VoiceMeeter Strip 索引（Banana 中 VAIO 为 3） | 3 |
| Auto Connect On Start | 启动时自动连接 | true |
| Default Mode | 默认输出模式 | A_HeadphoneOnly |
| Enable Auto Reconnect | 启用自动重连 | true |
| Connection Check Interval | 连接检查间隔（秒） | 5 |
| Reconnect Interval | 重连尝试间隔（秒） | 3 |
| Max Reconnect Attempts | 最大重连次数（0=无限） | 0 |

## VoiceMeeter Strip 索引参考

| 索引 | VoiceMeeter Banana |
|------|-------------------|
| 0 | Hardware Input 1 |
| 1 | Hardware Input 2 |
| 2 | Hardware Input 3 |
| **3** | **VoiceMeeter Input (VAIO)** |
| 4 | VoiceMeeter AUX |

## API 参考

### AudioOutputController

```csharp
// 属性
AudioOutputMode CurrentMode { get; }
bool IsConnected { get; }
bool IsReconnecting { get; }
int StripIndex { get; set; }

// 方法
bool Connect();
void Disconnect();
bool SetMode(AudioOutputMode mode);
bool SetModeA();
bool SetModeB();
bool ToggleMode();
void SyncFromVoiceMeeter();
void ForceReconnect();
```

### VoiceMeeterAPI

```csharp
// 连接
static bool Login();
static void Logout();
static bool CheckConnection();

// 参数控制
static bool SetParameter(string paramName, float value);
static bool GetParameter(string paramName, out float value);

// Strip 控制
static bool SetStripA1(int stripIndex, bool enabled);
static bool SetStripA2(int stripIndex, bool enabled);
static bool SetStripMute(int stripIndex, bool muted);
static bool SetStripGain(int stripIndex, float gainDb);
```

### AudioLogger

```csharp
// 日志
static AudioLogger Instance { get; }
void Log(LogLevel level, string message, string context);
void Debug(string message, string context = "AudioControl");
void Info(string message, string context = "AudioControl");
void Warning(string message, string context = "AudioControl");
void Error(string message, string context = "AudioControl");

// 导出
string Export();
void Clear();
```

## 许可证

MIT License
