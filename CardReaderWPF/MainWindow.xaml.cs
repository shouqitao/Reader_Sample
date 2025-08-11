using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CardReaderLib;
using CardReaderLib.Models;
using CardReaderLib.Exceptions;
using CardReaderWPF.Services;

namespace CardReaderWPF
{
    public partial class MainWindow : Window
    {
        private DeviceConnectionService _connectionService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isAutoReading = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            LogMessage("程序启动完成");
        }

        private void InitializeServices()
        {
            try
            {
                // 确保照片目录存在
                var photoPath = Path.Combine(Environment.CurrentDirectory, "Photos");
                if (!Directory.Exists(photoPath))
                {
                    Directory.CreateDirectory(photoPath);
                }

                // 初始化连接服务
                _connectionService = new DeviceConnectionService();
                _connectionService.StatusChanged += OnConnectionStatusChanged;
                
                UpdateConnectionStatus("服务已初始化");
            }
            catch (Exception ex)
            {
                var result = ErrorHandlingService.HandleError(ex, "初始化服务");
                LogError($"初始化失败: {result.UserMessage}");
            }
        }

        #region 连接管理

        /// <summary>
        /// 连接状态变化事件处理
        /// </summary>
        private void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateConnectionStatus(_connectionService.GetStatusDescription());
                
                var isConnected = _connectionService.IsConnected;
                EnableCardReaderButtons(isConnected);
                
                // 更新连接按钮状态
                if (isConnected)
                {
                    btnConnect.Content = "断开连接";
                    btnConnect.Background = System.Windows.Media.Brushes.OrangeRed;
                    LogMessage($"设备连接成功 - {_connectionService.GetConnectionDetails()}");
                }
                else if (e.NewStatus == DeviceConnectionService.ConnectionStatus.Disconnected)
                {
                    btnConnect.Content = "连接设备";
                    btnConnect.Background = System.Windows.Media.Brushes.Green;
                    if (e.OldStatus == DeviceConnectionService.ConnectionStatus.Connected)
                    {
                        LogMessage("设备已断开连接");
                    }
                }
                else if (e.NewStatus == DeviceConnectionService.ConnectionStatus.Error)
                {
                    btnConnect.Content = "重新连接";
                    btnConnect.Background = System.Windows.Media.Brushes.Red;
                    LogError("设备连接失败，将尝试自动重连");
                }
                else if (e.NewStatus == DeviceConnectionService.ConnectionStatus.Reconnecting)
                {
                    btnConnect.Content = "重连中...";
                    btnConnect.Background = System.Windows.Media.Brushes.Orange;
                    LogMessage($"正在尝试重新连接... ({_connectionService.GetStatusDescription()})");
                }
            });
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_connectionService.IsConnected)
            {
                DisconnectDevice();
                return;
            }

            await ConnectDevice();
        }

        private async Task ConnectDevice()
        {
            try
            {
                ShowLoading("正在连接设备...");
                
                // 更新配置
                bool useUsb = cmbConnectionType.SelectedIndex == 0;
                
                var config = new CardReaderConfig
                {
                    UseUsb = useUsb,
                    UsbPort = 1001,
                    SerialPort = 1,
                    TimeoutSeconds = 10,
                    AutoBeep = true,
                    PhotoSavePath = Path.Combine(Environment.CurrentDirectory, "Photos")
                };

                // 使用连接服务进行连接
                await _connectionService.ConnectAsync(config);
            }
            catch (Exception ex)
            {
                // 使用错误处理服务处理异常
                var result = ErrorHandlingService.HandleError(ex, "设备连接");
                
                // 如果用户选择重试，再次尝试连接
                if (result.UserClickedRetry)
                {
                    await Task.Delay(1000); // 等待一秒后重试
                    await ConnectDevice();
                }
            }
            finally
            {
                HideLoading();
            }
        }

        private void DisconnectDevice()
        {
            try
            {
                StopAllOperations();
                _connectionService.Disconnect();
            }
            catch (Exception ex)
            {
                var result = ErrorHandlingService.HandleError(ex, "断开连接");
                LogError($"断开连接时发生异常: {result.UserMessage}");
            }
        }

        private void EnableCardReaderButtons(bool enabled)
        {
            btnReadIdCard.IsEnabled = enabled;
            btnReadIdCardHD.IsEnabled = enabled;
            btnReadBankContact.IsEnabled = enabled;
            btnReadBankContactless.IsEnabled = enabled;
            btnReadSocialCard.IsEnabled = enabled;
            btnReadM1Card.IsEnabled = enabled;
            btnReadMagCard.IsEnabled = enabled;
            btnScanQR.IsEnabled = enabled;
            btnAutoRead.IsEnabled = enabled;
            btnBeep.IsEnabled = enabled;
        }

        private void StopAllOperations()
        {
            _cancellationTokenSource?.Cancel();
            _isAutoReading = false;
            btnStopAutoRead.IsEnabled = false;
            btnStopScan.IsEnabled = false;
            btnAutoRead.IsEnabled = _connectionService.IsConnected;
            btnScanQR.IsEnabled = _connectionService.IsConnected;
        }

        #endregion

        #region 身份证读取

        private async void BtnReadIdCard_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                ShowLoading("正在读取身份证...");
                LogMessage("开始读取身份证(通用API)");

                var result = await Task.Run(() => _connectionService.CardReader.ReadIdCard());
                
                if (result.IsSuccess)
                {
                    DisplayIdCardInfo(result.IdCardInfo);
                    LogMessage("身份证读取成功");
                }
                else
                {
                    var errorResult = ErrorHandlingService.HandleError(
                        new CardReadException(result.ErrorMessage, result.ReturnCode), 
                        "读取身份证");
                    ClearCardInfo();
                    
                    if (errorResult.UserClickedRetry)
                    {
                        await Task.Delay(500);
                        BtnReadIdCard_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                var result = ErrorHandlingService.HandleError(ex, "读取身份证");
                ClearCardInfo();
                
                if (result.UserClickedRetry)
                {
                    await Task.Delay(500);
                    BtnReadIdCard_Click(sender, e);
                }
            }
            finally
            {
                HideLoading();
            }
        }

        private async void BtnReadIdCardHD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading("正在读取身份证(华大API)...");
                LogMessage("开始读取身份证(华大API)");

                var result = await Task.Run(() => _cardReader.ReadIdCardHD());
                
                if (result.IsSuccess)
                {
                    DisplayIdCardInfo(result.IdCardInfo);
                    LogMessage("身份证读取成功(华大API)");
                }
                else
                {
                    LogError($"身份证读取失败(华大API): {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"读取身份证时发生异常(华大API): {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                HideLoading();
            }
        }

        private void DisplayIdCardInfo(IdCardInfo idInfo)
        {
            if (idInfo == null) return;

            var info = $"=== 身份证信息 ===\n";
            info += $"姓名: {idInfo.Name ?? "未知"}\n";
            info += $"性别: {idInfo.Sex ?? "未知"}\n";
            info += $"民族: {idInfo.Nation ?? "未知"}\n";
            info += $"出生日期: {idInfo.Birth ?? "未知"}\n";
            info += $"住址: {idInfo.Address ?? "未知"}\n";
            info += $"身份证号码: {idInfo.CertNo ?? "未知"}\n";
            info += $"签发机关: {idInfo.Department ?? "未知"}\n";
            info += $"有效期起: {idInfo.EffectDate ?? "未知"}\n";
            info += $"有效期止: {idInfo.ExpireDate ?? "未知"}\n";
            
            if (!string.IsNullOrEmpty(idInfo.CardId))
            {
                info += $"身份证ID: {idInfo.CardId}\n";
            }
            
            if (!string.IsNullOrEmpty(idInfo.PhotoPath))
            {
                info += $"照片路径: {idInfo.PhotoPath}\n";
                LoadIdPhoto(idInfo.PhotoPath);
            }

            txtCardInfo.Text = info;
        }

        private void LoadIdPhoto(string photoPath)
        {
            try
            {
                if (File.Exists(photoPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgIdPhoto.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                LogError($"加载身份证照片失败: {ex.Message}");
            }
        }

        #endregion

        #region 银行卡读取

        private async void BtnReadBankContact_Click(object sender, RoutedEventArgs e)
        {
            await ReadBankCard(false, "接触式");
        }

        private async void BtnReadBankContactless_Click(object sender, RoutedEventArgs e)
        {
            await ReadBankCard(true, "非接触式");
        }

        private async Task ReadBankCard(bool contactless, string type)
        {
            if (!CheckConnection()) return;

            try
            {
                ShowLoading($"正在读取{type}银行卡...");
                LogMessage($"开始读取{type}银行卡");

                var result = await Task.Run(() => _cardReader.ReadBankCard(contactless));
                
                if (result.IsSuccess && result.BankCardInfo != null)
                {
                    var bankInfo = result.BankCardInfo;
                    var info = $"=== {type}银行卡信息 ===\n";
                    info += $"银行卡号: {bankInfo.CardNo ?? "未知"}\n";
                    info += $"读取时间: {bankInfo.ReadTime:yyyy-MM-dd HH:mm:ss}\n";
                    info += $"读取方式: {type}\n";
                    if (!string.IsNullOrEmpty(bankInfo.RawData))
                    {
                        info += $"原始数据: {bankInfo.RawData}\n";
                    }

                    txtCardInfo.Text = info;
                    txtBankCardNo.Text = $"{type}: {bankInfo.CardNo ?? "未知"}";
                    LogMessage($"{type}银行卡读取成功");
                }
                else
                {
                    LogError($"{type}银行卡读取失败: {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"读取{type}银行卡时发生异常: {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                HideLoading();
            }
        }

        #endregion

        #region 其他卡片读取

        private async void BtnReadSocialCard_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                ShowLoading("正在读取社保卡...");
                LogMessage("开始读取社保卡");

                var result = await Task.Run(() => _cardReader.ReadSocialSecurityCard());
                
                if (result.IsSuccess && result.SocialSecurityInfo != null)
                {
                    var socialInfo = result.SocialSecurityInfo;
                    var info = $"=== 社保卡信息 ===\n";
                    info += $"社保卡号: {socialInfo.CardNo ?? "未知"}\n";
                    info += $"姓名: {socialInfo.Name ?? "未知"}\n";
                    info += $"性别: {socialInfo.Sex ?? "未知"}\n";
                    info += $"出生日期: {socialInfo.Birth ?? "未知"}\n";
                    info += $"身份证号: {socialInfo.CertNo ?? "未知"}\n";

                    txtCardInfo.Text = info;
                    LogMessage("社保卡读取成功");
                }
                else
                {
                    LogError($"社保卡读取失败: {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"读取社保卡时发生异常: {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                HideLoading();
            }
        }

        private async void BtnReadM1Card_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                ShowLoading("正在读取M1卡...");
                LogMessage("开始读取M1卡");

                var result = await Task.Run(() => _cardReader.ReadM1Card());
                
                if (result.IsSuccess && result.M1CardInfo != null)
                {
                    var m1Info = result.M1CardInfo;
                    var info = $"=== M1卡信息 ===\n";
                    info += $"卡号(16进制): {m1Info.CardNoHex ?? "未知"}\n";
                    info += $"卡号(10进制): {m1Info.CardNoDecimal ?? "未知"}\n";

                    txtCardInfo.Text = info;
                    txtM1CardNo.Text = $"M1卡: {m1Info.CardNoDecimal ?? "未知"}";
                    LogMessage("M1卡读取成功");
                }
                else
                {
                    LogError($"M1卡读取失败: {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"读取M1卡时发生异常: {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                HideLoading();
            }
        }

        private async void BtnReadMagCard_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                ShowLoading("正在读取磁条卡...");
                LogMessage("开始读取磁条卡，请刷卡...");

                var result = await Task.Run(() => _cardReader.ReadMagneticCard());
                
                if (result.IsSuccess && result.MagneticCardInfo != null)
                {
                    var magInfo = result.MagneticCardInfo;
                    var info = $"=== 磁条卡信息 ===\n";
                    info += $"卡号: {magInfo.CardNo ?? "未知"}\n";
                    info += $"轨道: {magInfo.TrackNumber}\n";
                    info += $"原始数据: {magInfo.TrackData ?? "未知"}\n";

                    txtCardInfo.Text = info;
                    LogMessage("磁条卡读取成功");
                }
                else
                {
                    LogError($"磁条卡读取失败: {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"读取磁条卡时发生异常: {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                HideLoading();
            }
        }

        #endregion

        #region 二维码扫描

        private async void BtnScanQR_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                btnScanQR.IsEnabled = false;
                btnStopScan.IsEnabled = true;
                _cancellationTokenSource = new CancellationTokenSource();

                ShowLoading("正在扫描二维码...");
                LogMessage("开始扫描二维码，请将二维码对准扫描区域");

                var result = await _cardReader.ScanQRCodeAsync(15);
                
                if (result.IsSuccess && result.QRCodeInfo != null)
                {
                    var qrInfo = result.QRCodeInfo;
                    var info = $"=== 二维码信息 ===\n";
                    info += $"扫描时间: {qrInfo.ScanTime:yyyy-MM-dd HH:mm:ss}\n";
                    info += $"二维码内容:\n{qrInfo.Content ?? "无内容"}\n";

                    txtCardInfo.Text = info;
                    LogMessage("二维码扫描成功");
                }
                else
                {
                    LogError($"二维码扫描失败: {result.ErrorMessage}");
                    ClearCardInfo();
                }
            }
            catch (Exception ex)
            {
                LogError($"扫描二维码时发生异常: {ex.Message}");
                ClearCardInfo();
            }
            finally
            {
                btnScanQR.IsEnabled = _isConnected;
                btnStopScan.IsEnabled = false;
                HideLoading();
            }
        }

        private void BtnStopScan_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            LogMessage("已停止二维码扫描");
        }

        #endregion

        #region 自动识别

        private async void BtnAutoRead_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                _isAutoReading = true;
                btnAutoRead.IsEnabled = false;
                btnStopAutoRead.IsEnabled = true;
                _cancellationTokenSource = new CancellationTokenSource();

                LogMessage("开始自动识别模式，请放置卡片...");

                while (_isAutoReading && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        UpdateStatus("等待卡片...");
                        var result = await _cardReader.ReadCardAutoAsync(_cancellationTokenSource.Token);
                        
                        if (result.IsSuccess)
                        {
                            DisplayAutoReadResult(result);
                            LogMessage($"自动识别成功: {result.CardType}");
                            
                            // 短暂延时后继续
                            await Task.Delay(2000, _cancellationTokenSource.Token);
                        }
                        else if (result.ReturnCode != -1) // 忽略超时错误
                        {
                            LogError($"自动识别失败: {result.ErrorMessage}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogError($"自动识别过程中发生异常: {ex.Message}");
                        await Task.Delay(1000); // 出错时等待1秒
                    }
                }
            }
            finally
            {
                _isAutoReading = false;
                btnAutoRead.IsEnabled = _isConnected;
                btnStopAutoRead.IsEnabled = false;
                UpdateStatus("就绪");
                LogMessage("自动识别模式已停止");
            }
        }

        private void BtnStopAutoRead_Click(object sender, RoutedEventArgs e)
        {
            _isAutoReading = false;
            _cancellationTokenSource?.Cancel();
            LogMessage("正在停止自动识别...");
        }

        private void DisplayAutoReadResult(CardReadResult result)
        {
            string info = $"=== 自动识别结果 ===\n";
            info += $"卡片类型: {GetCardTypeDescription(result.CardType)}\n";
            info += $"识别时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

            switch (result.CardType)
            {
                case CardType.IdCard:
                    if (result.IdCardInfo != null)
                    {
                        info += $"姓名: {result.IdCardInfo.Name ?? "未知"}\n";
                        info += $"身份证号: {result.IdCardInfo.CertNo ?? "未知"}\n";
                        if (!string.IsNullOrEmpty(result.IdCardInfo.PhotoPath))
                        {
                            LoadIdPhoto(result.IdCardInfo.PhotoPath);
                        }
                    }
                    break;

                case CardType.SocialSecurityCard:
                    if (result.SocialSecurityInfo != null)
                    {
                        info += $"姓名: {result.SocialSecurityInfo.Name ?? "未知"}\n";
                        info += $"社保卡号: {result.SocialSecurityInfo.CardNo ?? "未知"}\n";
                    }
                    break;

                case CardType.ContactBankCard:
                case CardType.ContactlessBankCard:
                    if (result.BankCardInfo != null)
                    {
                        info += $"银行卡号: {result.BankCardInfo.CardNo ?? "未知"}\n";
                        txtBankCardNo.Text = $"自动识别: {result.BankCardInfo.CardNo ?? "未知"}";
                    }
                    break;

                case CardType.M1Card:
                    if (result.M1CardInfo != null)
                    {
                        info += $"M1卡号: {result.M1CardInfo.CardNoDecimal ?? "未知"}\n";
                        txtM1CardNo.Text = $"自动识别: {result.M1CardInfo.CardNoDecimal ?? "未知"}";
                    }
                    break;

                case CardType.MagneticCard:
                    if (result.MagneticCardInfo != null)
                    {
                        info += $"磁条卡号: {result.MagneticCardInfo.CardNo ?? "未知"}\n";
                    }
                    break;

                case CardType.QRCode:
                    if (result.QRCodeInfo != null)
                    {
                        info += $"二维码内容: {result.QRCodeInfo.Content ?? "无内容"}\n";
                    }
                    break;
            }

            txtCardInfo.Text = info;
        }

        private string GetCardTypeDescription(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.IdCard:
                    return "身份证";
                case CardType.SocialSecurityCard:
                    return "社保卡";
                case CardType.ContactBankCard:
                    return "接触式银行卡";
                case CardType.ContactlessBankCard:
                    return "非接触式银行卡";
                case CardType.M1Card:
                    return "M1卡";
                case CardType.MagneticCard:
                    return "磁条卡";
                case CardType.QRCode:
                    return "二维码";
                default:
                    return "未知卡片";
            }
        }

        #endregion

        #region 设备控制

        private void BtnBeep_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            try
            {
                _cardReader.Beep(100);
                LogMessage("蜂鸣测试完成");
            }
            catch (Exception ex)
            {
                LogError($"蜂鸣测试失败: {ex.Message}");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
            txtCardInfo.Clear();
            txtBankCardNo.Text = "银行卡号将显示在这里";
            txtM1CardNo.Text = "M1卡号将显示在这里";
            imgIdPhoto.Source = null;
            LogMessage("日志已清空");
        }

        #endregion

        #region UI辅助方法

        private bool CheckConnection()
        {
            if (!_connectionService.IsConnected)
            {
                var result = MessageBox.Show(
                    "设备未连接。是否现在连接设备？",
                    "连接检查",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    _ = ConnectDevice(); // 异步连接，不等待结果
                }
                return false;
            }
            return true;
        }

        private void ShowLoading(string message)
        {
            txtLoadingMessage.Text = message;
            loadingOverlay.Visibility = Visibility.Visible;
        }

        private void HideLoading()
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateConnectionStatus(string status)
        {
            txtConnectionStatus.Text = status;
        }

        private void UpdateStatus(string status)
        {
            txtStatus.Text = status;
        }

        private void ClearCardInfo()
        {
            txtCardInfo.Text = "读取的卡片信息将在此显示...";
            imgIdPhoto.Source = null;
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}\n";
            
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(logEntry);
                txtLog.ScrollToEnd();
            });
        }

        private void LogError(string error)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] ❌ {error}\n";
            
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(logEntry);
                txtLog.ScrollToEnd();
            });
        }

        #endregion

        #region 窗口事件

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                StopAllOperations();
                _connectionService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭程序时发生异常: {ex.Message}");
            }
            
            base.OnClosing(e);
        }

        #endregion
    }
}