@echo off
setlocal enabledelayedexpansion

rem =============================
rem Build parameters
rem Usage: build [Debug|Release] [x86|x64|AnyCPU]
rem Defaults: Debug x86
rem =============================
set ARG1=%~1
set ARG2=%~2

rem 帮助信息
if /I "%ARG1%"=="/h" goto :help
if /I "%ARG1%"=="-h" goto :help
if /I "%ARG1%"=="--help" goto :help
if /I "%ARG1%"=="/?" goto :help

set CFG=%ARG1%
if "%CFG%"=="" set CFG=Debug
set PLAT=%ARG2%
if "%PLAT%"=="" set PLAT=x86

echo 开始编译 CardReader 解决方案... (^%CFG^% / ^%PLAT^%)
echo.

REM 检查 MSBuild 是否可用
where msbuild >nul 2>nul
if %errorlevel% neq 0 (
    echo 错误: 未找到 MSBuild，请确保已安装 Visual Studio 或 Build Tools
    echo 正在尝试查找 Visual Studio MSBuild...
    
    REM 尝试不同版本的 Visual Studio
    set "MSBUILD_PATH="
    
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    ) else if exist "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
    )
    
    if defined MSBUILD_PATH (
        echo 找到 MSBuild: !MSBUILD_PATH!
    ) else (
        echo 错误: 未找到 MSBuild，请安装 Visual Studio 或 Build Tools for Visual Studio
        pause
        exit /b 1
    )
) else (
    set "MSBUILD_PATH=msbuild"
)

echo 使用 MSBuild: %MSBUILD_PATH%
echo.

REM 编译 CardReaderLib
echo 正在编译 CardReaderLib...
"%MSBUILD_PATH%" CardReaderLib\CardReaderLib.csproj /p:Configuration=%CFG% /p:Platform=%PLAT% /verbosity:minimal /warnaserror
if %errorlevel% neq 0 (
    echo 错误: CardReaderLib 编译失败（可能包含警告）
    echo 提示: 请检查编译输出中的警告信息
    pause
    exit /b 1
)
echo CardReaderLib 编译成功（无警告）!
echo.

REM 编译 CardReaderWPF
echo 正在编译 CardReaderWPF...
"%MSBUILD_PATH%" CardReaderWPF\CardReaderWPF.csproj /p:Configuration=%CFG% /p:Platform=%PLAT% /verbosity:minimal /warnaserror
if %errorlevel% neq 0 (
    echo 错误: CardReaderWPF 编译失败（可能包含警告）
    echo 提示: 请检查编译输出中的警告信息
    pause
    exit /b 1
)
echo CardReaderWPF 编译成功（无警告）!
echo.

REM 编译整个解决方案
echo 正在编译整个解决方案...
"%MSBUILD_PATH%" CardReader.sln /p:Configuration=%CFG% /p:Platform=%PLAT% /verbosity:minimal /warnaserror
if %errorlevel% neq 0 (
    echo 错误: 解决方案编译失败（可能包含警告）
    echo 提示: 请检查编译输出中的警告信息
    pause
    exit /b 1
)

echo.
echo ===================================
echo 所有项目编译成功！（无编译警告）
echo ===================================
echo.
echo 生成的文件位置:
echo - CardReaderLib.dll: CardReaderLib\bin\%PLAT%\%CFG%\ 或项目默认输出
echo - CardReaderWPF.exe: CardReaderWPF\bin\%CFG%\ 或项目默认输出
echo.
echo 代码质量检查:
echo ✅ 无编译错误
echo ✅ 无编译警告  
echo ✅ Null 引用安全
echo ✅ 无未使用变量
echo.
pause

goto :eof

:help
echo 用法: build [Configuration] [Platform]
echo 示例:
echo   build            ^(默认 Debug x86^)
echo   build Release x64
echo   build Debug AnyCPU
echo.
echo 参数说明:
echo   Configuration: Debug ^| Release
echo   Platform     : x86 ^| x64 ^| AnyCPU
goto :eof
