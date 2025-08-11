# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个多层次的读卡器应用程序解决方案，展示了从传统 Windows Forms 到现代 WPF 架构的演进。项目基于 .NET Framework 4.8，通过 P/Invoke 调用本机 DLL 实现与各种读卡器设备的交互。

## 解决方案架构

### 主要解决方案
- **CardReader.sln** - 主推荐解决方案，包含完整的三层架构
- **Reader_Sample.sln** - 原始 WinForms 示例解决方案

### 三层架构设计
```
CardReaderWPF (现代化UI层)
       ↓ 依赖
CardReaderLib (业务逻辑层/API抽象层)
       ↓ 依赖
Native DLLs (硬件驱动层)
```

### 核心项目组件

#### 1. Reader_Sample (原始 WinForms 项目)
- **Form1.cs** - 主窗体，包含所有读卡功能的事件处理
- **HdCardDll.cs** - 华大身份证读卡器 API 封装（ATMS 命名空间）
- **dev.cs/dev-hd-100.cs** - 通用/华大设备接口封装
- **专用窗体** - sfz.cs, m1.cs, CPU.cs, 15693.cs（按卡片类型分离）

#### 2. CardReaderLib (统一类库)
- **CardReaderManager.cs** - 中央管理器，提供统一 API
- **Core/CardReaderNativeApi.cs** - P/Invoke 本机 API 封装
- **Models/CardInfo.cs** - 强类型数据模型（IdCardInfo, BankCardInfo 等）
- **Exceptions/CardReaderExceptions.cs** - 自定义异常体系
- **Examples/CardReaderExamples.cs** - API 使用示例代码

#### 3. CardReaderWPF (现代化 WPF 应用)
- **MainWindow.xaml/.xaml.cs** - 响应式现代化界面
- **Services/ErrorHandlingService.cs** - 智能错误处理和用户友好提示
- **Services/DeviceConnectionService.cs** - 设备连接状态管理和自动重连

## 开发命令

### 构建项目
```bash
# 推荐：使用自动化构建脚本（包含 MSBuild 路径自动检测）
.\build.bat

# 手动构建主解决方案
msbuild CardReader.sln /p:Configuration=Debug /p:Platform=x86

# 分别构建各项目
msbuild CardReaderLib\CardReaderLib.csproj /p:Configuration=Debug /p:Platform=x86
msbuild CardReaderWPF\CardReaderWPF.csproj /p:Configuration=Debug /p:Platform=x86
```

### 运行项目
```bash
# WPF 现代化版本（推荐）
.\CardReaderWPF\bin\Debug\CardReaderWPF.exe

# 原始 WinForms 版本
.\bin\Debug\Reader_Sample.exe
```

### 测试和质量检查
```bash
# build.bat 自动执行以下检查：
# - 零编译警告构建（/warnaserror）
# - Null 引用安全检查
# - 未使用变量检测
```

## 核心技术架构

### 硬件抽象层
- **P/Invoke 封装** - 通过 System.Runtime.InteropServices 调用本机 API
- **异常安全** - `[HandleProcessCorruptedStateExceptions]` 处理硬件访问异常
- **资源管理** - IDisposable 模式确保设备连接正确关闭

### 设备连接管理（CardReaderWPF 独有）
- **连接状态机** - Disconnected → Connecting → Connected → Error/Reconnecting
- **心跳检测** - 5 秒间隔自动检测设备状态
- **自动重连** - 最多 3 次重连尝试，2 秒延迟间隔
- **状态事件** - 连接状态变化的实时 UI 反馈

### 错误处理机制（CardReaderWPF 独有）
- **异常映射** - 技术异常到用户友好消息的智能转换
- **严重程度分级** - Info/Warning/Error/Critical 四级分类
- **重试机制** - 用户选择重试，自动处理临时性错误
- **技术日志** - 开发调试的详细错误记录

## 支持的读卡器功能

### 卡片类型
1. **身份证读取** - 华大 API (HDstdapi.dll) 和通用 API 双重支持
2. **银行卡读取** - 接触式/非接触式，支持卡号和基本信息
3. **社保卡读取** - 医保社保信息提取
4. **M1 卡读取** - Mifare One 卡片扇区读取
5. **磁条卡刷取** - 磁条银行卡信息读取  
6. **二维码扫描** - 扫码识别功能
7. **自动识别** - 智能判断卡片类型并选择读取方式

### 关键 API 接口
```csharp
// 设备控制
ICC_Reader_Open()         // 打开设备连接
ICC_Reader_Close()        // 关闭设备连接  
ICC_SelscetScan()         // 自动识别卡片类型

// 读卡功能
PICC_Reader_ReadIDMsg()   // 身份证信息读取
ICC_GetBankCardNo()       // 银行卡号获取
Mifare_Read()             // M1 卡扇区读取
```

## 配置和部署

### 平台要求
- **目标平台** - 必须使用 x86 编译（兼容 32 位读卡器驱动）
- **运行环境** - .NET Framework 4.8
- **C# 版本** - 7.3（确保兼容性）
- **Visual Studio** - 2017 或更高版本

### 依赖文件
- **SSSE32.dll** - 通用读卡器驱动（必须）
- **HDstdapi.dll** - 华大身份证读卡器驱动（可选）
- **读卡器硬件驱动** - 根据具体设备安装

### 连接配置
- **USB 连接** - 端口号 1001（推荐）
- **串口连接** - 端口号 1-16（传统方式）
- **超时设置** - 默认 10 秒，可通过 CardReaderConfig 自定义

## 开发注意事项

### 异常处理最佳实践
- 始终使用 try-catch 包装硬件操作
- 优先捕获 `DeviceConnectionException` 和 `CardReadException`
- WPF 项目中使用 `ErrorHandlingService.HandleError()` 提供用户友好提示
- 技术异常通过 `ErrorHandlingService.LogTechnicalError()` 记录

### 字符编码处理
- 中文信息使用 `System.Text.Encoding.Default`
- 身份证姓名和地址需要特殊编码处理
- JSON 序列化时注意编码一致性

### 资源管理
- 每次读卡操作后必须调用设备关闭方法
- 使用 `using` 语句确保 CardReaderManager 正确释放
- WPF 项目在窗口关闭时释放 DeviceConnectionService

### 项目演进建议
- **新项目** - 优先使用 CardReaderWPF + CardReaderLib 组合
- **现有项目集成** - 直接引用 CardReaderLib，享受统一 API
- **原始示例** - Reader_Sample 作为参考实现，理解底层调用机制

## 故障排除

### 常见编译问题
- 平台不匹配：确保所有项目都设置为 x86 平台
- 警告阻止编译：build.bat 使用 /warnaserror，需要零警告编译
- 路径问题：使用 build.bat 自动检测 MSBuild 路径

### 运行时设备问题
- 设备未连接：WPF 版本会智能提示连接步骤
- 驱动问题：检查设备管理器中读卡器状态
- 端口占用：关闭其他可能占用读卡器的程序

### 最佳开发流程
1. 使用 CardReader.sln 作为主开发解决方案
2. 运行 build.bat 确保零警告编译
3. 优先在 CardReaderWPF 中测试新功能
4. 参考 CardReaderLib/Examples 了解 API 用法
5. 查阅项目根目录的技术文档获取详细信息