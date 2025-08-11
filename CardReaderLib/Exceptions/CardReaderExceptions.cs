using System;

namespace CardReaderLib.Exceptions
{
    /// <summary>
    /// 读卡器异常基类
    /// </summary>
    public class CardReaderException : Exception
    {
        public int ReturnCode { get; }

        public CardReaderException(string message) : base(message)
        {
        }

        public CardReaderException(string message, int returnCode) : base(message)
        {
            ReturnCode = returnCode;
        }

        public CardReaderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CardReaderException(string message, int returnCode, Exception innerException) : base(message, innerException)
        {
            ReturnCode = returnCode;
        }
    }

    /// <summary>
    /// 设备连接异常
    /// </summary>
    public class DeviceConnectionException : CardReaderException
    {
        public DeviceConnectionException(string message) : base(message)
        {
        }

        public DeviceConnectionException(string message, int returnCode) : base(message, returnCode)
        {
        }

        public DeviceConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 卡片读取异常
    /// </summary>
    public class CardReadException : CardReaderException
    {
        public CardReadException(string message) : base(message)
        {
        }

        public CardReadException(string message, int returnCode) : base(message, returnCode)
        {
        }

        public CardReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 设备初始化异常
    /// </summary>
    public class DeviceInitializationException : CardReaderException
    {
        public DeviceInitializationException(string message) : base(message)
        {
        }

        public DeviceInitializationException(string message, int returnCode) : base(message, returnCode)
        {
        }

        public DeviceInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}