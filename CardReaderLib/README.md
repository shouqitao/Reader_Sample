# CardReaderLib - 通用读卡器类库

## 概述

CardReaderLib 是一个基于 .NET Framework 4.8 的通用读卡器帮助类库，专为简化各种读卡设备的集成而设计。该类库封装了复杂的硬件调用，提供了简洁易用的 API，支持多种类型的卡片读取。

## 支持的卡片类型

- **身份证** - 支持华大和通用API两种读取方式
- **银行卡** - 支持接触式和非接触式读取
- **社保卡** - 医保社保信息读取
- **M1卡** - Mifare One 卡片读取
- **磁条卡** - 磁条银行卡刷取
- **二维码** - 扫码功能
- **自动识别** - 智能识别卡片类型

## 主要特性

✅ **简单易用** - 提供统一的 API 接口，降低学习成本  
✅ **异步支持** - 支持异步操作，避免UI阻塞  
✅ **异常处理** - 完善的异常处理机制，提高应用稳定性  
✅ **配置灵活** - 丰富的配置选项，满足不同场景需求  
✅ **自动资源管理** - 实现IDisposable接口，自动释放资源  
✅ **多平台兼容** - 支持x86平台，兼容大部分读卡器驱动  

## 快速开始

### 1. 基本使用

```csharp
using CardReaderLib;

// 创建读卡器管理器
using (var cardReader = new CardReaderManager())
{
    // 初始化
    cardReader.Initialize();
    
    // 自动识别并读取卡片
    var result = await cardReader.ReadCardAutoAsync();
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"读取成功: {result.CardType}");
    }
}
```

### 2. 自定义配置

```csharp
var config = new CardReaderConfig
{
    UseUsb = true,              // 使用USB连接
    UsbPort = 1001,             // USB端口号
    TimeoutSeconds = 10,        // 超时时间
    AutoBeep = true,            // 自动蜂鸣
    PhotoSavePath = @"C:\Photos" // 照片保存路径
};

using (var cardReader = new CardReaderManager(config))
{
    cardReader.Initialize();
    // ... 使用读卡器
}
```

### 3. 读取特定类型卡片

```csharp
// 读取身份证
var idResult = cardReader.ReadIdCard();
if (idResult.IsSuccess)
{
    var info = idResult.IdCardInfo;
    Console.WriteLine($"姓名: {info.Name}");
    Console.WriteLine($"身份证号: {info.CertNo}");
}

// 读取银行卡
var bankResult = cardReader.ReadBankCard(contactless: true);
if (bankResult.IsSuccess)
{
    Console.WriteLine($"银行卡号: {bankResult.BankCardInfo.CardNo}");
}

// 扫描二维码
var qrResult = await cardReader.ScanQRCodeAsync();
if (qrResult.IsSuccess)
{
    Console.WriteLine($"二维码内容: {qrResult.QRCodeInfo.Content}");
}
```

## API 参考

### CardReaderManager 类

主要的读卡器管理类，提供所有读卡功能。

#### 方法

| 方法 | 描述 | 返回类型 |
|------|------|----------|
| `Initialize()` | 初始化读卡器连接 | void |
| `Close()` | 关闭读卡器连接 | void |
| `Beep(byte duration)` | 蜂鸣提示 | void |
| `ReadCardAutoAsync()` | 自动识别并读取卡片 | Task\<CardReadResult\> |
| `ReadIdCard()` | 读取身份证（通用API） | CardReadResult |
| `ReadIdCardHD()` | 读取身份证（华大API） | CardReadResult |
| `ReadBankCard(bool contactless)` | 读取银行卡 | CardReadResult |
| `ReadSocialSecurityCard()` | 读取社保卡 | CardReadResult |
| `ReadM1Card()` | 读取M1卡 | CardReadResult |
| `ReadMagneticCard(int track)` | 读取磁条卡 | CardReadResult |
| `ScanQRCodeAsync(int timeout)` | 扫描二维码 | Task\<CardReadResult\> |

### CardReadResult 类

读卡操作的返回结果。

#### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `IsSuccess` | bool | 是否成功 |
| `CardType` | CardType | 卡片类型 |
| `ErrorMessage` | string | 错误消息 |
| `ReturnCode` | int | 返回码 |
| `IdCardInfo` | IdCardInfo | 身份证信息 |
| `BankCardInfo` | BankCardInfo | 银行卡信息 |
| `SocialSecurityInfo` | SocialSecurityCardInfo | 社保卡信息 |
| `M1CardInfo` | M1CardInfo | M1卡信息 |
| `MagneticCardInfo` | MagneticCardInfo | 磁条卡信息 |
| `QRCodeInfo` | QRCodeInfo | 二维码信息 |

### CardReaderConfig 类

读卡器配置类。

#### 属性

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `UsbPort` | int | 1001 | USB端口号 |
| `SerialPort` | int | 1 | 串口号 |
| `UseUsb` | bool | true | 是否使用USB连接 |
| `TimeoutSeconds` | int | 10 | 读卡超时时间（秒） |
| `PhotoSavePath` | string | 当前目录 | 照片保存路径 |
| `AutoBeep` | bool | false | 是否自动蜂鸣 |

## 异常处理

类库提供了完善的异常处理机制：

```csharp
try
{
    using (var cardReader = new CardReaderManager())
    {
        cardReader.Initialize();
        var result = await cardReader.ReadCardAutoAsync();
        // 处理结果
    }
}
catch (DeviceConnectionException ex)
{
    // 设备连接异常
    Console.WriteLine($"设备连接失败: {ex.Message}");
}
catch (CardReadException ex)
{
    // 读卡异常
    Console.WriteLine($"读卡失败: {ex.Message}");
}
catch (DeviceInitializationException ex)
{
    // 设备初始化异常
    Console.WriteLine($"设备初始化失败: {ex.Message}");
}
```

## 系统要求

- **.NET Framework 4.8** 或更高版本
- **Windows 操作系统**
- **x86 平台** (32位)
- **读卡器驱动程序**:
  - `SSSE32.dll` - 通用读卡器驱动
  - `HDstdapi.dll` - 华大身份证读卡器驱动

## 部署说明

1. **编译项目** - 确保使用 x86 平台编译
2. **复制 DLL 文件** - 将驱动 DLL 文件复制到输出目录
3. **权限设置** - 确保应用程序有足够权限访问硬件设备
4. **测试连接** - 在部署环境中测试读卡器连接

## 故障排查

### 常见问题

**Q: 设备连接失败**  
A: 检查驱动是否正确安装，确认设备连接正常，尝试不同的端口设置。

**Q: 读卡失败**  
A: 确认卡片放置正确，检查卡片是否损坏，增加超时时间。

**Q: DLL 找不到**  
A: 确保驱动 DLL 文件在应用程序目录或系统PATH中。

**Q: 平台不兼容**  
A: 确保使用 x86 平台编译，读卡器驱动通常为32位。

### 调试技巧

```csharp
// 启用详细错误信息
var result = await cardReader.ReadCardAutoAsync();
if (!result.IsSuccess)
{
    Console.WriteLine($"错误码: {result.ReturnCode}");
    Console.WriteLine($"错误信息: {result.ErrorMessage}");
    if (result.RawData is Exception ex)
    {
        Console.WriteLine($"异常详情: {ex}");
    }
}
```

## 版本历史

- **v1.0.0** - 初始版本，支持所有基本读卡功能

## 许可证

本项目采用宽松的开源许可证，您可以自由使用、修改和分发。

---

## 联系支持

如需技术支持或有任何问题，请通过以下方式联系：

- 提交 Issue 到项目仓库
- 查看示例代码获取更多用法
- 参考 CLAUDE.md 了解项目架构