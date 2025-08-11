using Microsoft.VisualStudio.TestTools.UnitTesting;
using CardReaderLib;

namespace CardReaderLib.Tests
{
    [TestClass]
    public class CardReaderConfigTests
    {
        [TestMethod]
        public void Defaults_Should_Be_Stable()
        {
            var cfg = new CardReaderConfig();
            Assert.AreEqual(1001, cfg.UsbPort);
            Assert.AreEqual(1, cfg.SerialPort);
            Assert.IsTrue(cfg.UseUsb);
            Assert.AreEqual(10, cfg.TimeoutSeconds);
            Assert.IsFalse(cfg.AutoBeep);
            Assert.IsFalse(string.IsNullOrWhiteSpace(cfg.PhotoSavePath));
        }
    }
}
