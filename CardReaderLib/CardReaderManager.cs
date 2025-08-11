using System;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CardReaderLib.Models;
using CardReaderLib.Core;
using CardReaderLib.Exceptions;

namespace CardReaderLib
{
    /// <summary>
    /// 读卡器配置
    /// </summary>
    public class CardReaderConfig
    {
        /// <summary>USB端口号，默认1001</summary>
        public int UsbPort { get; set; }
        
        /// <summary>串口号，默认1</summary>
        public int SerialPort { get; set; }
        
        /// <summary>是否使用USB连接，默认true</summary>
        public bool UseUsb { get; set; }
        
        /// <summary>读卡超时时间（秒），默认10秒</summary>
        public int TimeoutSeconds { get; set; }
        
        /// <summary>照片保存路径，默认当前目录</summary>
        public string PhotoSavePath { get; set; }
        
        /// <summary>是否自动蜂鸣，默认false</summary>
        public bool AutoBeep { get; set; }

        public CardReaderConfig()
        {
            UsbPort = 1001;
            SerialPort = 1;
            UseUsb = true;
            TimeoutSeconds = 10;
            PhotoSavePath = Environment.CurrentDirectory;
            AutoBeep = false;
        }
    }

    /// <summary>
    /// 通用读卡器管理类
    /// </summary>
    public class CardReaderManager : IDisposable
    {
        private readonly CardReaderConfig _config;
        private int _readerHandle = -1;
        private bool _isInitialized = false;
        private readonly object _lockObject = new object();

        public CardReaderManager(CardReaderConfig config = null)
        {
            _config = config ?? new CardReaderConfig();
        }

        /// <summary>
        /// 初始化读卡器
        /// </summary>
        public void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                    return;

                try
                {
                    var deviceName = new StringBuilder(_config.UseUsb ? "USB1" : $"COM{_config.SerialPort}");
                    _readerHandle = CardReaderNativeApi.ICC_Reader_Open(deviceName);
                    
                    if (_readerHandle <= 0)
                    {
                        throw new DeviceConnectionException($"无法连接到读卡器设备，返回码: {_readerHandle}", _readerHandle);
                    }

                    _isInitialized = true;
                }
                catch (Exception ex) when (!(ex is CardReaderException))
                {
                    throw new DeviceInitializationException("读卡器初始化失败", ex);
                }
            }
        }

        /// <summary>
        /// 关闭读卡器连接
        /// </summary>
        public void Close()
        {
            lock (_lockObject)
            {
                if (_isInitialized && _readerHandle > 0)
                {
                    CardReaderNativeApi.ICC_Reader_Close(_readerHandle);
                    _readerHandle = -1;
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// 蜂鸣提示
        /// </summary>
        /// <param name="duration">持续时间（0-255）</param>
        public void Beep(byte duration = 50)
        {
            EnsureInitialized();
            CardReaderNativeApi.ICC_PosBeep(_readerHandle, duration);
        }

        /// <summary>
        /// 自动识别并读取卡片
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读卡结果</returns>
        public async Task<CardReadResult> ReadCardAutoAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            return await Task.Run(() =>
            {
                try
                {
                    byte[] pCodeInfo = new byte[3000];
                    int blent = -1;
                    int ret = CardReaderNativeApi.ICC_SelscetScan(_readerHandle, pCodeInfo, ref blent);

                    if (ret < 0)
                    {
                        return new CardReadResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "自动识别卡片失败",
                            ReturnCode = ret
                        };
                    }

                    return ProcessAutoScanResult(ret, pCodeInfo, blent);
                }
                catch (Exception ex)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"读卡过程中发生异常: {ex.Message}",
                        RawData = ex
                    };
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 读取身份证信息（华大API）
        /// </summary>
        /// <returns>身份证读取结果</returns>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public CardReadResult ReadIdCardHD()
        {
            try
            {
                int port = _config.UseUsb ? _config.UsbPort : _config.SerialPort;
                
                port = CardReaderNativeApi.HD_InitComm(port);
                if (port < 0)
                {
                    CardReaderNativeApi.HD_CloseComm();
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "华大身份证读卡器初始化失败",
                        ReturnCode = port
                    };
                }

                port = CardReaderNativeApi.HD_Authenticate(1);
                if (port != 0)
                {
                    CardReaderNativeApi.HD_CloseComm();
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "身份证认证失败",
                        ReturnCode = port
                    };
                }

                long result = CardReaderNativeApi.HD_ReadCard();
                if (result < 0)
                {
                    CardReaderNativeApi.HD_CloseComm();
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "读取身份证失败",
                        ReturnCode = (int)result
                    };
                }

                var idCardInfo = new IdCardInfo
                {
                    Name = CardReaderNativeApi.GetName(),
                    CertNo = CardReaderNativeApi.GetCertNo(),
                    Sex = CardReaderNativeApi.GetSex(),
                    Nation = CardReaderNativeApi.GetNation(),
                    Birth = CardReaderNativeApi.GetBirth(),
                    Address = CardReaderNativeApi.GetAddress(),
                    Department = CardReaderNativeApi.GetDepartemt(),
                    EffectDate = CardReaderNativeApi.GetEffectDate(),
                    ExpireDate = CardReaderNativeApi.GetExpireDate()
                };

                // 保存照片
                string photoPath = System.IO.Path.Combine(_config.PhotoSavePath, $"id_photo_{DateTime.Now:yyyyMMddHHmmss}.bmp");
                CardReaderNativeApi.GetBmpFile(photoPath);
                idCardInfo.PhotoPath = photoPath;

                CardReaderNativeApi.HD_CloseComm();

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = CardType.IdCard,
                    IdCardInfo = idCardInfo
                };
            }
            catch (Exception ex)
            {
                CardReaderNativeApi.HD_CloseComm();
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取身份证时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 读取身份证信息（通用API）
        /// </summary>
        /// <returns>身份证读取结果</returns>
        public CardReadResult ReadIdCard()
        {
            EnsureInitialized();

            try
            {
                var pBmpFile = new StringBuilder();
                var pName = new StringBuilder(100);
                var pSex = new StringBuilder(100);
                var pNation = new StringBuilder(100);
                var pBirth = new StringBuilder(100);
                var pAddress = new StringBuilder(100);
                var pCertNo = new StringBuilder(100);
                var pDepartment = new StringBuilder(100);
                var pEffectData = new StringBuilder(100);
                var pExpire = new StringBuilder(100);
                var pErrMsg = new StringBuilder(100);

                string photoPath = System.IO.Path.Combine(_config.PhotoSavePath, $"id_photo_{DateTime.Now:yyyyMMddHHmmss}.bmp");
                pBmpFile.Append(photoPath);

                int result = CardReaderNativeApi.PICC_Reader_ReadIDMsg(_readerHandle, pBmpFile, pName, pSex, 
                    pNation, pBirth, pAddress, pCertNo, pDepartment, pEffectData, pExpire, pErrMsg);

                if (result != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"读取身份证失败: {pErrMsg}",
                        ReturnCode = result
                    };
                }

                // 获取身份证ID
                byte[] uid = new byte[20];
                StringBuilder sUID = new StringBuilder(30);
                int uidResult = CardReaderNativeApi.PICC_Reader_Read_CardID(_readerHandle, uid);
                if (uidResult > 0)
                {
                    CardReaderNativeApi.HexToStr(uid, uidResult, sUID);
                }

                var idCardInfo = new IdCardInfo
                {
                    Name = pName.ToString(),
                    Sex = pSex.ToString(),
                    Nation = pNation.ToString(),
                    Birth = pBirth.ToString(),
                    Address = pAddress.ToString(),
                    CertNo = pCertNo.ToString(),
                    Department = pDepartment.ToString(),
                    EffectDate = pEffectData.ToString(),
                    ExpireDate = pExpire.ToString(),
                    PhotoPath = photoPath,
                    CardId = sUID.ToString()
                };

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = CardType.IdCard,
                    IdCardInfo = idCardInfo
                };
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取身份证时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 读取银行卡信息
        /// </summary>
        /// <param name="contactless">是否为非接触式读取</param>
        /// <returns>银行卡读取结果</returns>
        public CardReadResult ReadBankCard(bool contactless = true)
        {
            EnsureInitialized();

            try
            {
                byte[] kh = new byte[255];
                byte[] kh_len = new byte[2];
                int iType = contactless ? 1 : 0;

                int result = CardReaderNativeApi.ICC_GetBankCardNo(_readerHandle, iType, kh, kh_len);

                if (result != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "读取银行卡失败",
                        ReturnCode = result
                    };
                }

                string rawData = System.Text.Encoding.Default.GetString(kh);
                string cardNo = rawData.Split('=')[0];

                var bankCardInfo = new BankCardInfo
                {
                    CardNo = cardNo,
                    CardType = contactless ? CardType.ContactlessBankCard : CardType.ContactBankCard,
                    ReadTime = DateTime.Now,
                    RawData = rawData
                };

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = bankCardInfo.CardType,
                    BankCardInfo = bankCardInfo
                };
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取银行卡时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 读取社保卡信息
        /// </summary>
        /// <returns>社保卡读取结果</returns>
        public CardReadResult ReadSocialSecurityCard()
        {
            EnsureInitialized();

            try
            {
                var pCardNo = new StringBuilder(19);
                var pCertNo = new StringBuilder(100);
                var pName = new StringBuilder(100);
                byte[] pSex = new byte[255];
                byte[] pBirth = new byte[255];

                int result = CardReaderNativeApi.PICC_Reader_SSCardInfo1(_readerHandle, pCardNo, pCertNo, pName, pSex, pBirth);

                if (result != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "读取社保卡失败",
                        ReturnCode = result
                    };
                }

                var pSex1 = new StringBuilder(100);
                var pBirth1 = new StringBuilder(100);
                CardReaderNativeApi.HexToStr(pSex, 1, pSex1);
                CardReaderNativeApi.HexToStr(pBirth, 4, pBirth1);

                var socialSecurityInfo = new SocialSecurityCardInfo
                {
                    CardNo = pCardNo.ToString(),
                    CertNo = pCertNo.ToString(),
                    Name = pName.ToString(),
                    Sex = pSex1.ToString(),
                    Birth = pBirth1.ToString()
                };

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = CardType.SocialSecurityCard,
                    SocialSecurityInfo = socialSecurityInfo
                };
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取社保卡时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 读取M1卡信息
        /// </summary>
        /// <returns>M1卡读取结果</returns>
        public CardReadResult ReadM1Card()
        {
            EnsureInitialized();

            try
            {
                // 设置为A卡模式
                int ret = CardReaderNativeApi.PICC_Reader_SetTypeA(_readerHandle);
                if (ret != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "设置为A卡模式失败",
                        ReturnCode = ret
                    };
                }

                // 请求卡片
                ret = CardReaderNativeApi.PICC_Reader_Request(_readerHandle);
                if (ret != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "请求卡片失败",
                        ReturnCode = ret
                    };
                }

                // 防碰撞
                byte[] UID = new byte[5];
                ret = CardReaderNativeApi.PICC_Reader_anticoll(_readerHandle, UID);
                if (ret != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "防碰撞失败",
                        ReturnCode = ret
                    };
                }

                // 选卡
                ret = CardReaderNativeApi.PICC_Reader_Select(_readerHandle, 0x41);
                if (ret != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "选卡失败",
                        ReturnCode = ret
                    };
                }

                // 转换UID为16进制字符串
                var strBuilder = new StringBuilder();
                for (int index = 0; index < 4; index++)
                {
                    strBuilder.Append(((int)UID[index]).ToString("X2"));
                }

                long cardNoDecimal = Convert.ToInt64(strBuilder.ToString(), 16);

                var m1CardInfo = new M1CardInfo
                {
                    CardNoHex = strBuilder.ToString(),
                    CardNoDecimal = cardNoDecimal.ToString(),
                    UID = UID
                };

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = CardType.M1Card,
                    M1CardInfo = m1CardInfo
                };
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取M1卡时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 读取磁条卡信息
        /// </summary>
        /// <param name="track">轨道号，默认2轨</param>
        /// <param name="timeoutSeconds">超时时间（秒），默认10秒</param>
        /// <returns>磁条卡读取结果</returns>
        public CardReadResult ReadMagneticCard(int track = 2, int timeoutSeconds = 10)
        {
            EnsureInitialized();

            try
            {
                var cardInfo = new StringBuilder(100);
                byte[] rlen = new byte[2];

                int result = CardReaderNativeApi.Rcard(_readerHandle, (byte)timeoutSeconds, track, rlen, cardInfo);

                if (result != 0)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "读取磁条卡失败",
                        ReturnCode = result
                    };
                }

                string trackData = cardInfo.ToString();
                string cardNo = trackData.Contains("=") ? trackData.Split('=')[0] : trackData;

                var magneticCardInfo = new MagneticCardInfo
                {
                    TrackData = trackData,
                    TrackNumber = track,
                    CardNo = cardNo
                };

                if (_config.AutoBeep)
                    Beep();

                return new CardReadResult
                {
                    IsSuccess = true,
                    CardType = CardType.MagneticCard,
                    MagneticCardInfo = magneticCardInfo
                };
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"读取磁条卡时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        /// <summary>
        /// 扫描二维码
        /// </summary>
        /// <param name="timeoutSeconds">超时时间（秒），默认10秒</param>
        /// <returns>二维码扫描结果</returns>
        public async Task<CardReadResult> ScanQRCodeAsync(int timeoutSeconds = 10)
        {
            EnsureInitialized();

            return await Task.Run(() =>
            {
                try
                {
                    var cardInfo = new StringBuilder(1000);

                    // 设置扫码状态
                    CardReaderNativeApi.ICC_CtrScanCode(_readerHandle, 2, 0);

                    int result = CardReaderNativeApi.ICC_ScanCodeSM(_readerHandle, (byte)timeoutSeconds, cardInfo, 0);

                    if (result < 0)
                    {
                        return new CardReadResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "扫码失败",
                            ReturnCode = result
                        };
                    }

                    var qrCodeInfo = new QRCodeInfo
                    {
                        Content = cardInfo.ToString(),
                        ScanTime = DateTime.Now
                    };

                    if (_config.AutoBeep)
                        Beep();

                    return new CardReadResult
                    {
                        IsSuccess = true,
                        CardType = CardType.QRCode,
                        QRCodeInfo = qrCodeInfo
                    };
                }
                catch (Exception ex)
                {
                    return new CardReadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"扫描二维码时发生异常: {ex.Message}",
                        RawData = ex
                    };
                }
            });
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("读卡器未初始化，请先调用Initialize方法");
            }
        }

        private CardReadResult ProcessAutoScanResult(int cardType, byte[] data, int dataLength)
        {
            try
            {
                switch (cardType)
                {
                    case 1: // 社保卡
                        return ReadSocialSecurityCard();

                    case 2: // M1卡
                        var ID = new StringBuilder(100);
                        CardReaderNativeApi.HexToStr(data, dataLength, ID);
                        return new CardReadResult
                        {
                            IsSuccess = true,
                            CardType = CardType.M1Card,
                            M1CardInfo = new M1CardInfo { CardNoHex = ID.ToString() }
                        };

                    case 3: // 磁条卡
                        var magneticID = new StringBuilder(100);
                        CardReaderNativeApi.HexToStr(data, dataLength, magneticID);
                        return new CardReadResult
                        {
                            IsSuccess = true,
                            CardType = CardType.MagneticCard,
                            MagneticCardInfo = new MagneticCardInfo { CardNo = magneticID.ToString() }
                        };

                    case 4: // 二维码
                        string qrContent = System.Text.Encoding.UTF8.GetString(data);
                        return new CardReadResult
                        {
                            IsSuccess = true,
                            CardType = CardType.QRCode,
                            QRCodeInfo = new QRCodeInfo { Content = qrContent, ScanTime = DateTime.Now }
                        };

                    case 5: // 身份证
                        return ReadIdCard();

                    case 6: // 银行卡
                        return ReadBankCard();

                    default:
                        return new CardReadResult
                        {
                            IsSuccess = false,
                            CardType = CardType.Unknown,
                            ErrorMessage = $"未知的卡片类型: {cardType}"
                        };
                }
            }
            catch (Exception ex)
            {
                return new CardReadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"处理自动扫描结果时发生异常: {ex.Message}",
                    RawData = ex
                };
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}