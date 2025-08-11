# Repository Guidelines

## 项目结构与模块
- `Reader_Sample/`：WinForms 示例（.NET Framework 4.8），窗体含 `Form1`、`m1`、`CPU`、`15693`、`sfz`；产物输出到各自 `bin/`。
- `CardReaderLib/`：通用库，目录含 `Core/`、`Models/`、`Exceptions/`、`Examples/`，核心类为 `CardReaderManager.cs`。
- `CardReaderWPF/`：WPF 示例界面（`MainWindow.xaml`、`Services/`）。
- 根目录：`CardReader.sln`、`build.bat`、各项目文件；构建中间产物在 `obj/`。

## 构建、运行与开发命令
- 一键构建（默认 Debug/x86）：`build.bat`（自动查找 MSBuild，按库→WPF→解决方案顺序构建）。
- 解决方案构建：`msbuild CardReader.sln /p:Configuration=Debug /p:Platform=x86 /v:m`
- 单项目构建：`msbuild CardReaderLib\CardReaderLib.csproj /p:Configuration=Debug /p:Platform=x86`
- build.bat 传参：`build.bat Release x64`（或 `build.bat Debug x86`）。
- Release/x64 示例：`msbuild CardReader.sln /p:Configuration=Release /p:Platform=x64`
- 运行 WinForms：`Reader_Sample\bin\Debug\Reader_Sample.exe`
- 运行 WPF：`CardReaderWPF\bin\Debug\CardReaderWPF.exe`
- 清理：`msbuild CardReader.sln /t:Clean`

## 代码风格与命名
- C# 7.3，.NET Framework 4.8；缩进 4 空格，UTF-8。
- 命名：类型/方法/属性 PascalCase；局部/参数 camelCase；私有字段 `_camelCase`（例：`_readerHandle`）。
- 结构：每文件单一类型；命名空间与目录对应（如 `CardReaderLib.Models`）；显式访问修饰符与早返回。
- 文档：公共 API 使用 XML 注释；涉及设备行为/错误码可补充简要中文注释。

## 测试指南
- 建议新增 `CardReaderLib.Tests`（MSTest/NUnit）。测试类 `*Tests`，方法 `MethodName_Should...`。
- 运行：VS Test Explorer；或 `vstest.console.exe CardReaderLib.Tests\bin\Debug\CardReaderLib.Tests.dll`。
- 重点覆盖：`CardReaderManager` 主要流程、错误码分支、P/Invoke 边界与异常处理。

## 提交与 Pull Request
- 提交：一次一事，祈使语，简明中/英皆可。示例：`fix: HD reader init on x86`。
- PR：清晰描述、复现/验证步骤（含设备与驱动版本）、关联 Issue；UI 改动附截图；`app.config` 变更需注明新增键值。

## 安全与配置
- 不提交设备 ID、许可证、个人照片等敏感信息；`app.config` 放示例值。
- 若改动设备初始化或传输方式（USB/COM），请在 PR 中注明依赖驱动与版本。
 
## 目录变更说明（维护者须知）
- WinForms 项目已整体迁入 `Reader_Sample/` 文件夹；未纳入项目的历史/辅助源码移动至 `Reader_Sample/Extras/`。
- 解决方案 `CardReader.sln` 已更新 WinForms 项目路径为 `Reader_Sample\Reader_Sample.csproj`。
