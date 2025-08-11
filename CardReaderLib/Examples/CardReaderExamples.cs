using System;
using System.Threading.Tasks;
using CardReaderLib;
using CardReaderLib.Models;

namespace CardReaderLib.Examples
{
    /// <summary>
    /// 读卡器使用示例
    /// </summary>
    public class CardReaderExamples
    {
        /// <summary>
        /// 基本使用示例
        /// </summary>
        public static async Task BasicUsageExample()
        {
            // 1. 创建读卡器配置
            var config = new CardReaderConfig
            {
                UseUsb = true,              // 使用USB连接
                UsbPort = 1001,             // USB端口号
                TimeoutSeconds = 10,        // 超时10秒
                AutoBeep = true,            // 自动蜂鸣
                PhotoSavePath = @"C:\Photos" // 照片保存路径
            };

            // 2. 创建读卡器管理器
            using (var cardReader = new CardReaderManager(config))
            {
                try
                {
                    // 3. 初始化读卡器
                    cardReader.Initialize();
                    Console.WriteLine("读卡器初始化成功");

                    // 4. 自动识别并读取卡片
                    Console.WriteLine("请放置卡片...");
                    var result = await cardReader.ReadCardAutoAsync();
                    
                    if (result.IsSuccess)
                    {
                        Console.WriteLine($"读卡成功! 卡片类型: {result.CardType}");
                        DisplayCardInfo(result);
                    }
                    else
                    {
                        Console.WriteLine($"读卡失败: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生异常: {ex.Message}");
                }
            } // 自动调用Dispose方法关闭连接
        }

        /// <summary>
        /// 读取特定类型卡片示例
        /// </summary>
        public static void SpecificCardExample()
        {
            using (var cardReader = new CardReaderManager())
            {
                try
                {
                    cardReader.Initialize();

                    // 读取身份证
                    Console.WriteLine("正在读取身份证...");
                    var idResult = cardReader.ReadIdCard();
                    if (idResult.IsSuccess)
                    {
                        var idInfo = idResult.IdCardInfo;
                        Console.WriteLine($"姓名: {idInfo.Name}");
                        Console.WriteLine($"身份证号: {idInfo.CertNo}");
                        Console.WriteLine($"性别: {idInfo.Sex}");
                        Console.WriteLine($"出生日期: {idInfo.Birth}");
                    }

                    // 读取银行卡
                    Console.WriteLine("正在读取银行卡...");
                    var bankResult = cardReader.ReadBankCard(contactless: true);
                    if (bankResult.IsSuccess)
                    {
                        var bankInfo = bankResult.BankCardInfo;
                        Console.WriteLine($"银行卡号: {bankInfo.CardNo}");
                        Console.WriteLine($"读取时间: {bankInfo.ReadTime}");
                    }

                    // 读取M1卡
                    Console.WriteLine("正在读取M1卡...");
                    var m1Result = cardReader.ReadM1Card();
                    if (m1Result.IsSuccess)
                    {
                        var m1Info = m1Result.M1CardInfo;
                        Console.WriteLine($"M1卡号(16进制): {m1Info.CardNoHex}");
                        Console.WriteLine($"M1卡号(10进制): {m1Info.CardNoDecimal}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 异步二维码扫描示例
        /// </summary>
        public static async Task QRCodeScanExample()
        {
            using (var cardReader = new CardReaderManager())
            {
                try
                {
                    cardReader.Initialize();

                    Console.WriteLine("请展示二维码到扫描区域...");
                    var qrResult = await cardReader.ScanQRCodeAsync(timeoutSeconds: 15);
                    
                    if (qrResult.IsSuccess)
                    {
                        var qrInfo = qrResult.QRCodeInfo;
                        Console.WriteLine($"二维码内容: {qrInfo.Content}");
                        Console.WriteLine($"扫描时间: {qrInfo.ScanTime}");
                    }
                    else
                    {
                        Console.WriteLine($"扫码失败: {qrResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 华大身份证读卡器示例
        /// </summary>
        public static void HuadaIdCardExample()
        {
            using (var cardReader = new CardReaderManager())
            {
                try
                {
                    // 注意：华大身份证读卡器不需要调用Initialize()
                    Console.WriteLine("正在使用华大API读取身份证...");
                    var result = cardReader.ReadIdCardHD();
                    
                    if (result.IsSuccess)
                    {
                        var idInfo = result.IdCardInfo;
                        Console.WriteLine("=== 身份证信息 ===");
                        Console.WriteLine($"姓名: {idInfo.Name}");
                        Console.WriteLine($"性别: {idInfo.Sex}");
                        Console.WriteLine($"民族: {idInfo.Nation}");
                        Console.WriteLine($"出生日期: {idInfo.Birth}");
                        Console.WriteLine($"住址: {idInfo.Address}");
                        Console.WriteLine($"身份证号码: {idInfo.CertNo}");
                        Console.WriteLine($"签发机关: {idInfo.Department}");
                        Console.WriteLine($"有效期: {idInfo.EffectDate} - {idInfo.ExpireDate}");
                        Console.WriteLine($"照片路径: {idInfo.PhotoPath}");
                    }
                    else
                    {
                        Console.WriteLine($"读取身份证失败: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 连续读卡示例
        /// </summary>
        public static async Task ContinuousReadExample()
        {
            using (var cardReader = new CardReaderManager(new CardReaderConfig { AutoBeep = true }))
            {
                try
                {
                    cardReader.Initialize();
                    Console.WriteLine("连续读卡模式已启动，按任意键退出...");

                    while (!Console.KeyAvailable)
                    {
                        Console.WriteLine("请放置卡片...");
                        var result = await cardReader.ReadCardAutoAsync();
                        
                        if (result.IsSuccess)
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 读取到 {result.CardType} 卡片");
                            DisplayCardInfo(result);
                        }
                        else if (result.ReturnCode != -1) // 忽略超时错误
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 读卡失败: {result.ErrorMessage}");
                        }

                        await Task.Delay(1000); // 等待1秒后继续
                    }

                    Console.ReadKey(); // 清除按键
                    Console.WriteLine("退出连续读卡模式");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 显示卡片信息
        /// </summary>
        /// <param name="result">读卡结果</param>
        private static void DisplayCardInfo(CardReadResult result)
        {
            switch (result.CardType)
            {
                case CardType.IdCard:
                    var idInfo = result.IdCardInfo;
                    Console.WriteLine($"  姓名: {idInfo.Name}");
                    Console.WriteLine($"  身份证号: {idInfo.CertNo}");
                    Console.WriteLine($"  性别: {idInfo.Sex}");
                    break;

                case CardType.SocialSecurityCard:
                    var ssInfo = result.SocialSecurityInfo;
                    Console.WriteLine($"  姓名: {ssInfo.Name}");
                    Console.WriteLine($"  社保卡号: {ssInfo.CardNo}");
                    Console.WriteLine($"  身份证号: {ssInfo.CertNo}");
                    break;

                case CardType.ContactBankCard:
                case CardType.ContactlessBankCard:
                    var bankInfo = result.BankCardInfo;
                    Console.WriteLine($"  银行卡号: {bankInfo.CardNo}");
                    Console.WriteLine($"  读取方式: {(result.CardType == CardType.ContactBankCard ? "接触式" : "非接触式")}");
                    break;

                case CardType.M1Card:
                    var m1Info = result.M1CardInfo;
                    Console.WriteLine($"  M1卡号(16进制): {m1Info.CardNoHex}");
                    Console.WriteLine($"  M1卡号(10进制): {m1Info.CardNoDecimal}");
                    break;

                case CardType.MagneticCard:
                    var magInfo = result.MagneticCardInfo;
                    Console.WriteLine($"  磁条卡号: {magInfo.CardNo}");
                    Console.WriteLine($"  轨道: {magInfo.TrackNumber}");
                    break;

                case CardType.QRCode:
                    var qrInfo = result.QRCodeInfo;
                    Console.WriteLine($"  二维码内容: {qrInfo.Content}");
                    Console.WriteLine($"  扫描时间: {qrInfo.ScanTime}");
                    break;
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 主函数示例
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== 读卡器类库使用示例 ===");
            Console.WriteLine("1. 基本使用示例");
            Console.WriteLine("2. 特定卡片读取示例");
            Console.WriteLine("3. 二维码扫描示例");
            Console.WriteLine("4. 华大身份证读卡器示例");
            Console.WriteLine("5. 连续读卡示例");
            Console.Write("请选择示例 (1-5): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    await BasicUsageExample();
                    break;
                case "2":
                    SpecificCardExample();
                    break;
                case "3":
                    await QRCodeScanExample();
                    break;
                case "4":
                    HuadaIdCardExample();
                    break;
                case "5":
                    await ContinuousReadExample();
                    break;
                default:
                    Console.WriteLine("无效选择");
                    break;
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}