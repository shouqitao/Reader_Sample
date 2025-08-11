using System;
using System.Threading;
using System.Threading.Tasks;
using CardReaderLib;
using CardReaderLib.Exceptions;

namespace CardReaderWPF.Services
{
    /// <summary>
    /// 设备连接状态管理服务
    /// </summary>
    public class DeviceConnectionService
    {
        /// <summary>
        /// 连接状态枚举
        /// </summary>
        public enum ConnectionStatus
        {
            Disconnected,   // 未连接
            Connecting,     // 连接中
            Connected,      // 已连接
            Error,          // 连接错误
            Reconnecting    // 重连中
        }

        private CardReaderManager _cardReader;
        private CardReaderConfig _config;
        private ConnectionStatus _status = ConnectionStatus.Disconnected;
        private Timer _heartbeatTimer;
        private Timer _reconnectTimer;
        private bool _autoReconnectEnabled = true;
        private int _reconnectAttempts = 0;
        private const int MaxReconnectAttempts = 3;
        private const int HeartbeatIntervalMs = 5000; // 5秒心跳检测
        private const int ReconnectDelayMs = 2000; // 2秒重连延迟

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// 当前连接状态
        /// </summary>
        public ConnectionStatus Status 
        { 
            get => _status;
            private set
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(oldStatus, value));
                }
            }
        }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => Status == ConnectionStatus.Connected;

        /// <summary>
        /// 当前读卡器实例
        /// </summary>
        public CardReaderManager CardReader => _cardReader;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceConnectionService()
        {
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        public async Task<bool> ConnectAsync(CardReaderConfig config)
        {
            if (Status == ConnectionStatus.Connecting || Status == ConnectionStatus.Reconnecting)
                return false;

            _config = config;
            Status = ConnectionStatus.Connecting;

            try
            {
                // 清理现有连接
                CleanupConnection();

                // 创建新的读卡器实例
                _cardReader = new CardReaderManager(config);
                
                // 在后台线程中初始化设备
                await Task.Run(() => _cardReader.Initialize());

                // 验证连接
                if (await VerifyConnectionAsync())
                {
                    Status = ConnectionStatus.Connected;
                    _reconnectAttempts = 0;
                    StartHeartbeat();
                    StopReconnectTimer();
                    return true;
                }
                else
                {
                    throw new DeviceConnectionException("设备连接验证失败", -1);
                }
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.Error;
                ErrorHandlingService.LogTechnicalError(ex, "设备连接");
                
                // 如果启用自动重连，开始重连尝试
                if (_autoReconnectEnabled && _reconnectAttempts < MaxReconnectAttempts)
                {
                    StartReconnectTimer();
                }
                
                throw;
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        public void Disconnect()
        {
            Status = ConnectionStatus.Disconnected;
            _autoReconnectEnabled = false;
            CleanupConnection();
            StopHeartbeat();
            StopReconnectTimer();
        }

        /// <summary>
        /// 启用/禁用自动重连
        /// </summary>
        public void SetAutoReconnect(bool enabled)
        {
            _autoReconnectEnabled = enabled;
            
            if (!enabled)
            {
                StopReconnectTimer();
            }
        }

        /// <summary>
        /// 验证设备连接状态
        /// </summary>
        public async Task<bool> VerifyConnectionAsync()
        {
            if (_cardReader == null)
                return false;

            try
            {
                // 尝试简单的设备操作来验证连接
                await Task.Run(() => _cardReader.Beep(50));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取连接状态描述
        /// </summary>
        public string GetStatusDescription()
        {
            switch (Status)
            {
                case ConnectionStatus.Disconnected:
                    return "未连接";
                case ConnectionStatus.Connecting:
                    return "连接中...";
                case ConnectionStatus.Connected:
                    return "已连接";
                case ConnectionStatus.Error:
                    return "连接错误";
                case ConnectionStatus.Reconnecting:
                    return $"重连中... (尝试 {_reconnectAttempts}/{MaxReconnectAttempts})";
                default:
                    return "未知状态";
            }
        }

        /// <summary>
        /// 获取连接详细信息
        /// </summary>
        public string GetConnectionDetails()
        {
            if (_config == null)
                return "无配置信息";

            var connectionType = _config.UseUsb ? "USB" : "串口";
            var port = _config.UseUsb ? _config.UsbPort.ToString() : _config.SerialPort.ToString();
            
            return $"{connectionType} - 端口: {port}";
        }

        /// <summary>
        /// 启动心跳检测
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatTimer = new Timer(async _ => await CheckHeartbeat(), null, 
                HeartbeatIntervalMs, HeartbeatIntervalMs);
        }

        /// <summary>
        /// 停止心跳检测
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        /// <summary>
        /// 心跳检测
        /// </summary>
        private async Task CheckHeartbeat()
        {
            if (Status != ConnectionStatus.Connected)
                return;

            var isConnected = await VerifyConnectionAsync();
            if (!isConnected)
            {
                Status = ConnectionStatus.Error;
                
                if (_autoReconnectEnabled && _reconnectAttempts < MaxReconnectAttempts)
                {
                    StartReconnectTimer();
                }
            }
        }

        /// <summary>
        /// 启动重连定时器
        /// </summary>
        private void StartReconnectTimer()
        {
            StopReconnectTimer();
            _reconnectTimer = new Timer(async _ => await AttemptReconnect(), null, 
                ReconnectDelayMs, Timeout.Infinite);
        }

        /// <summary>
        /// 停止重连定时器
        /// </summary>
        private void StopReconnectTimer()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        /// <summary>
        /// 尝试自动重连
        /// </summary>
        private async Task AttemptReconnect()
        {
            if (!_autoReconnectEnabled || _reconnectAttempts >= MaxReconnectAttempts)
                return;

            _reconnectAttempts++;
            Status = ConnectionStatus.Reconnecting;

            try
            {
                await ConnectAsync(_config);
            }
            catch (Exception ex)
            {
                ErrorHandlingService.LogTechnicalError(ex, $"自动重连尝试 {_reconnectAttempts}");
                
                if (_reconnectAttempts < MaxReconnectAttempts)
                {
                    // 延迟后再次尝试
                    StartReconnectTimer();
                }
                else
                {
                    Status = ConnectionStatus.Error;
                }
            }
        }

        /// <summary>
        /// 清理连接资源
        /// </summary>
        private void CleanupConnection()
        {
            try
            {
                _cardReader?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理连接时出错: {ex.Message}");
            }
            finally
            {
                _cardReader = null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// 连接状态变化事件参数
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public DeviceConnectionService.ConnectionStatus OldStatus { get; }
        public DeviceConnectionService.ConnectionStatus NewStatus { get; }

        public ConnectionStatusChangedEventArgs(
            DeviceConnectionService.ConnectionStatus oldStatus, 
            DeviceConnectionService.ConnectionStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}