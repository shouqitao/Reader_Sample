using System;
using System.Collections.Generic;
using System.Windows;
using CardReaderLib.Exceptions;

namespace CardReaderWPF.Services
{
    /// <summary>
    /// 错误处理服务 - 提供友好的错误处理和用户提示
    /// </summary>
    public class ErrorHandlingService
    {
        /// <summary>
        /// 错误严重程度
        /// </summary>
        public enum ErrorSeverity
        {
            Info,       // 信息提示
            Warning,    // 警告
            Error,      // 错误
            Critical    // 严重错误
        }

        /// <summary>
        /// 错误信息映射字典
        /// </summary>
        private static readonly Dictionary<Type, ErrorInfo> ErrorMappings = new Dictionary<Type, ErrorInfo>
        {
            {
                typeof(DeviceConnectionException),
                new ErrorInfo
                {
                    Title = "设备连接问题",
                    GetUserMessage = ex => GetConnectionErrorMessage(ex as DeviceConnectionException),
                    Severity = ErrorSeverity.Warning,
                    ShowRetryButton = true,
                    AutoRetryable = true
                }
            },
            {
                typeof(CardReadException),
                new ErrorInfo
                {
                    Title = "读卡失败",
                    GetUserMessage = ex => GetCardReadErrorMessage(ex as CardReadException),
                    Severity = ErrorSeverity.Warning,
                    ShowRetryButton = true,
                    AutoRetryable = false
                }
            },
            {
                typeof(DeviceInitializationException),
                new ErrorInfo
                {
                    Title = "设备初始化失败",
                    GetUserMessage = ex => GetInitializationErrorMessage(ex as DeviceInitializationException),
                    Severity = ErrorSeverity.Error,
                    ShowRetryButton = true,
                    AutoRetryable = true
                }
            }
        };

        /// <summary>
        /// 处理异常并显示用户友好的错误信息
        /// </summary>
        public static ErrorHandlingResult HandleError(Exception exception, string context = "")
        {
            if (exception == null)
                return new ErrorHandlingResult { Handled = false };

            var exceptionType = exception.GetType();
            
            // 查找最匹配的异常类型
            ErrorInfo errorInfo = null;
            foreach (var mapping in ErrorMappings)
            {
                if (mapping.Key.IsAssignableFrom(exceptionType))
                {
                    errorInfo = mapping.Value;
                    break;
                }
            }

            // 如果没有找到映射，使用默认处理
            if (errorInfo == null)
            {
                errorInfo = new ErrorInfo
                {
                    Title = "未知错误",
                    GetUserMessage = ex => $"发生了未预期的错误: {ex.Message}",
                    Severity = ErrorSeverity.Error,
                    ShowRetryButton = false,
                    AutoRetryable = false
                };
            }

            var userMessage = errorInfo.GetUserMessage(exception);
            if (!string.IsNullOrEmpty(context))
            {
                userMessage = $"{context}: {userMessage}";
            }

            var result = new ErrorHandlingResult
            {
                Handled = true,
                UserMessage = userMessage,
                Title = errorInfo.Title,
                Severity = errorInfo.Severity,
                ShowRetryButton = errorInfo.ShowRetryButton,
                AutoRetryable = errorInfo.AutoRetryable,
                Exception = exception
            };

            // 显示错误对话框
            ShowErrorDialog(result);

            return result;
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private static void ShowErrorDialog(ErrorHandlingResult result)
        {
            MessageBoxImage icon;
            switch (result.Severity)
            {
                case ErrorSeverity.Info:
                    icon = MessageBoxImage.Information;
                    break;
                case ErrorSeverity.Warning:
                    icon = MessageBoxImage.Warning;
                    break;
                case ErrorSeverity.Error:
                    icon = MessageBoxImage.Error;
                    break;
                case ErrorSeverity.Critical:
                    icon = MessageBoxImage.Stop;
                    break;
                default:
                    icon = MessageBoxImage.Warning;
                    break;
            }

            var buttons = result.ShowRetryButton ? MessageBoxButton.YesNo : MessageBoxButton.OK;
            var buttonText = result.ShowRetryButton ? "是否重试？" : "";
            var fullMessage = result.UserMessage + (string.IsNullOrEmpty(buttonText) ? "" : $"\n\n{buttonText}");

            var dialogResult = MessageBox.Show(fullMessage, result.Title, buttons, icon);
            result.UserClickedRetry = dialogResult == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 获取设备连接错误的用户友好消息
        /// </summary>
        private static string GetConnectionErrorMessage(DeviceConnectionException ex)
        {
            if (ex?.ReturnCode != 0)
            {
                switch (ex.ReturnCode)
                {
                    case -1:
                        return "设备未连接或连接断开。请检查:\n• 设备是否正确连接到电脑\n• USB线是否完好\n• 设备驱动是否正确安装";
                    case -2:
                        return "设备端口被占用。请检查:\n• 是否有其他程序正在使用读卡器\n• 尝试重新插拔设备";
                    case -3:
                        return "设备通信超时。请检查:\n• 设备是否正常工作\n• 重新连接设备";
                    default:
                        return $"设备连接失败 (错误代码: {ex.ReturnCode})。请检查设备连接和驱动程序。";
                }
            }
            return "无法连接到读卡器设备。请确保设备已正确连接并安装了驱动程序。";
        }

        /// <summary>
        /// 获取读卡错误的用户友好消息
        /// </summary>
        private static string GetCardReadErrorMessage(CardReadException ex)
        {
            if (ex?.ReturnCode != 0)
            {
                switch (ex.ReturnCode)
                {
                    case -4:
                        return "未检测到卡片。请确保:\n• 卡片已正确放置在读卡区域\n• 卡片方向正确\n• 卡片清洁无损坏";
                    case -5:
                        return "卡片读取超时。请:\n• 重新放置卡片\n• 确保卡片完全放入读卡区域";
                    case -6:
                        return "卡片数据错误或损坏。请:\n• 清洁卡片表面\n• 尝试其他卡片\n• 检查卡片是否损坏";
                    default:
                        return $"读卡失败 (错误代码: {ex.ReturnCode})。请重新放置卡片或尝试其他卡片。";
                }
            }
            return "读取卡片信息失败。请检查卡片是否正确放置，然后重试。";
        }

        /// <summary>
        /// 获取设备初始化错误的用户友好消息
        /// </summary>
        private static string GetInitializationErrorMessage(DeviceInitializationException ex)
        {
            if (ex?.ReturnCode != 0)
            {
                return $"设备初始化失败 (错误代码: {ex.ReturnCode})。请尝试:\n• 重新连接设备\n• 重启应用程序\n• 检查设备驱动程序";
            }
            return "设备初始化失败。请重新连接设备或重启应用程序。";
        }

        /// <summary>
        /// 记录错误日志（供开发人员使用）
        /// </summary>
        public static void LogTechnicalError(Exception exception, string context = "")
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 技术错误 - {context}\n" +
                           $"异常类型: {exception.GetType().Name}\n" +
                           $"错误消息: {exception.Message}\n" +
                           $"堆栈跟踪: {exception.StackTrace}\n";
            
            // 这里可以写入日志文件或输出到调试控制台
            System.Diagnostics.Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
    }

    /// <summary>
    /// 错误处理结果
    /// </summary>
    public class ErrorHandlingResult
    {
        public bool Handled { get; set; }
        public string UserMessage { get; set; }
        public string Title { get; set; }
        public ErrorHandlingService.ErrorSeverity Severity { get; set; }
        public bool ShowRetryButton { get; set; }
        public bool AutoRetryable { get; set; }
        public bool UserClickedRetry { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// 错误信息配置
    /// </summary>
    internal class ErrorInfo
    {
        public string Title { get; set; }
        public Func<Exception, string> GetUserMessage { get; set; }
        public ErrorHandlingService.ErrorSeverity Severity { get; set; }
        public bool ShowRetryButton { get; set; }
        public bool AutoRetryable { get; set; }
    }
}