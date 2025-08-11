using System;

namespace CardReaderLib.Models
{
    /// <summary>
    /// 卡片类型枚举
    /// </summary>
    public enum CardType
    {
        /// <summary>未知卡片</summary>
        Unknown = 0,
        /// <summary>居民身份证</summary>
        IdCard = 1,
        /// <summary>社保卡</summary>
        SocialSecurityCard = 2,
        /// <summary>M1卡</summary>
        M1Card = 3,
        /// <summary>磁条卡</summary>
        MagneticCard = 4,
        /// <summary>二维码</summary>
        QRCode = 5,
        /// <summary>银行卡（接触式）</summary>
        ContactBankCard = 6,
        /// <summary>银行卡（非接触式）</summary>
        ContactlessBankCard = 7,
        /// <summary>15693卡</summary>
        Card15693 = 8
    }

    /// <summary>
    /// 身份证信息
    /// </summary>
    public class IdCardInfo
    {
        /// <summary>姓名</summary>
        public string Name { get; set; }
        
        /// <summary>性别</summary>
        public string Sex { get; set; }
        
        /// <summary>民族</summary>
        public string Nation { get; set; }
        
        /// <summary>出生日期</summary>
        public string Birth { get; set; }
        
        /// <summary>住址</summary>
        public string Address { get; set; }
        
        /// <summary>身份证号码</summary>
        public string CertNo { get; set; }
        
        /// <summary>签发机关</summary>
        public string Department { get; set; }
        
        /// <summary>有效期起始日期</summary>
        public string EffectDate { get; set; }
        
        /// <summary>有效期截止日期</summary>
        public string ExpireDate { get; set; }
        
        /// <summary>照片文件路径</summary>
        public string PhotoPath { get; set; }
        
        /// <summary>身份证卡号</summary>
        public string CardId { get; set; }
    }

    /// <summary>
    /// 社保卡信息
    /// </summary>
    public class SocialSecurityCardInfo
    {
        /// <summary>社保卡号</summary>
        public string CardNo { get; set; }
        
        /// <summary>身份证号</summary>
        public string CertNo { get; set; }
        
        /// <summary>姓名</summary>
        public string Name { get; set; }
        
        /// <summary>性别</summary>
        public string Sex { get; set; }
        
        /// <summary>出生日期</summary>
        public string Birth { get; set; }
    }

    /// <summary>
    /// 银行卡信息
    /// </summary>
    public class BankCardInfo
    {
        /// <summary>银行卡号</summary>
        public string CardNo { get; set; }
        
        /// <summary>卡片类型（接触式/非接触式）</summary>
        public CardType CardType { get; set; }
        
        /// <summary>读取时间</summary>
        public DateTime ReadTime { get; set; }
        
        /// <summary>原始数据</summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// M1卡信息
    /// </summary>
    public class M1CardInfo
    {
        /// <summary>卡号（16进制）</summary>
        public string CardNoHex { get; set; }
        
        /// <summary>卡号（十进制）</summary>
        public string CardNoDecimal { get; set; }
        
        /// <summary>物理卡号</summary>
        public byte[] UID { get; set; }
    }

    /// <summary>
    /// 磁条卡信息
    /// </summary>
    public class MagneticCardInfo
    {
        /// <summary>磁条数据</summary>
        public string TrackData { get; set; }
        
        /// <summary>轨道号</summary>
        public int TrackNumber { get; set; }
        
        /// <summary>卡号</summary>
        public string CardNo { get; set; }
    }

    /// <summary>
    /// 二维码信息
    /// </summary>
    public class QRCodeInfo
    {
        /// <summary>二维码内容</summary>
        public string Content { get; set; }
        
        /// <summary>扫描时间</summary>
        public DateTime ScanTime { get; set; }
    }

    /// <summary>
    /// 通用卡片读取结果
    /// </summary>
    public class CardReadResult
    {
        /// <summary>是否成功</summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>卡片类型</summary>
        public CardType CardType { get; set; }
        
        /// <summary>错误消息</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>返回码</summary>
        public int ReturnCode { get; set; }
        
        /// <summary>身份证信息</summary>
        public IdCardInfo IdCardInfo { get; set; }
        
        /// <summary>社保卡信息</summary>
        public SocialSecurityCardInfo SocialSecurityInfo { get; set; }
        
        /// <summary>银行卡信息</summary>
        public BankCardInfo BankCardInfo { get; set; }
        
        /// <summary>M1卡信息</summary>
        public M1CardInfo M1CardInfo { get; set; }
        
        /// <summary>磁条卡信息</summary>
        public MagneticCardInfo MagneticCardInfo { get; set; }
        
        /// <summary>二维码信息</summary>
        public QRCodeInfo QRCodeInfo { get; set; }
        
        /// <summary>原始数据</summary>
        public object RawData { get; set; }
    }
}