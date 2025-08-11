using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CardReaderLib.Core
{
    /// <summary>
    /// 读卡器原生API封装
    /// </summary>
    internal static class CardReaderNativeApi
    {
        private const string SSSE32_DLL = "SSSE32.dll";
        private const string HD_STD_API_DLL = "HDstdapi.dll";

        #region 基础设备操作
        [DllImport(SSSE32_DLL, EntryPoint = "ICC_Reader_Open")]
        public static extern int ICC_Reader_Open(StringBuilder dev_Name);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_Reader_Close")]
        public static extern int ICC_Reader_Close(int ReaderHandle);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_PosBeep")]
        public static extern int ICC_PosBeep(int ReaderHandle, byte time);
        #endregion

        #region 身份证读取 - 华大API
        [DllImport(HD_STD_API_DLL)]
        public static extern int HD_InitComm(int iPort);

        [DllImport(HD_STD_API_DLL)]
        public static extern int HD_CloseComm();

        [DllImport(HD_STD_API_DLL)]
        public static extern int HD_Authenticate(int iType);

        [DllImport(HD_STD_API_DLL)]
        public static extern long HD_ReadCard();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetName();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetCertNo();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetSex();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetNation();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetBirth();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetAddress();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetDepartemt();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetEffectDate();

        [DllImport(HD_STD_API_DLL)]
        public static extern string GetExpireDate();

        [DllImport(HD_STD_API_DLL)]
        public static extern int GetBmpFile(string pBmpfilepath);
        #endregion

        #region 身份证读取 - 通用API
        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_ReadIDMsg")]
        public static extern int PICC_Reader_ReadIDMsg(int RHandle, StringBuilder pBmpFile, 
            StringBuilder pName, StringBuilder pSex, StringBuilder pNation, StringBuilder pBirth, 
            StringBuilder pAddress, StringBuilder pCertNo, StringBuilder pDepartment, 
            StringBuilder pEffectData, StringBuilder pExpire, StringBuilder pErrMsg);

        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_Read_CardID")]
        public static extern int PICC_Reader_Read_CardID(int ReaderHandle, byte[] UID);
        #endregion

        #region 银行卡读取
        [DllImport(SSSE32_DLL, EntryPoint = "ICC_GetBankCardNo")]
        public static extern int ICC_GetBankCardNo(int ReaderHandle, int nType, byte[] bankCardNo, byte[] kh_len);
        #endregion

        #region 社保卡读取
        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_SSCardInfo1")]
        public static extern int PICC_Reader_SSCardInfo1(int ReaderHandle, StringBuilder pSSCardID,
            StringBuilder pIDNum, StringBuilder pName, byte[] pSex, byte[] pBorn);
        #endregion

        #region M1卡操作
        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_SetTypeA")]
        public static extern int PICC_Reader_SetTypeA(int ReaderHandle);

        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_Request")]
        public static extern int PICC_Reader_Request(int ReaderHandle);

        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_anticoll")]
        public static extern int PICC_Reader_anticoll(int ReaderHandle, byte[] uid);

        [DllImport(SSSE32_DLL, EntryPoint = "PICC_Reader_Select")]
        public static extern int PICC_Reader_Select(int ReaderHandle, byte cardtype);
        #endregion

        #region 磁条卡读取
        [DllImport(SSSE32_DLL, EntryPoint = "Rcard")]
        public static extern int Rcard(int ReaderHandle, byte ctime, int track, byte[] rlen, StringBuilder data);
        #endregion

        #region 二维码扫描
        [DllImport(SSSE32_DLL, EntryPoint = "ICC_CtrScanCode")]
        public static extern int ICC_CtrScanCode(int ReaderHandle, int inFlae, int WFlag);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_ScanCodeSM")]
        public static extern int ICC_ScanCodeSM(int ReaderHandle, byte ctime, StringBuilder QRCodeInfo, int WFlag);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_ScanCodeStar")]
        public static extern int ICC_ScanCodeStar(int ReaderHandle, StringBuilder QRCodeInfo, int WFlag);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_ScanCodeRead")]
        public static extern int ICC_ScanCodeRead(int ReaderHandle, StringBuilder QRCodeInfo);
        #endregion

        #region 自动识别
        [DllImport(SSSE32_DLL, EntryPoint = "ICC_SelscetScan")]
        public static extern int ICC_SelscetScan(int ReaderHandle, byte[] pCodeInfo, ref int blent);

        [DllImport(SSSE32_DLL, EntryPoint = "ICC_StopSelscetScan")]
        public static extern int ICC_StopSelscetScan(int ReaderHandle);
        #endregion

        #region 工具函数
        [DllImport(SSSE32_DLL, EntryPoint = "StrToHex")]
        public static extern int StrToHex(StringBuilder strIn, int len, byte[] HexOut);

        [DllImport(SSSE32_DLL, EntryPoint = "HexToStr")]
        public static extern int HexToStr(byte[] strIn, int inLen, StringBuilder strOut);
        #endregion
    }
}