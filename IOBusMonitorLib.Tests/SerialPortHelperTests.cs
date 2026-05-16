using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Ports;

namespace IOBusMonitorLib.Tests
{
    [TestClass]
    public class SerialPortHelperTests
    {
        [TestMethod]
        public void GetSerialPortName_ReturnsEnumName()
        {
            Assert.AreEqual("COM7", SerialPortHelper.GetSerialPortName(SerialPortName.COM7));
        }

        [TestMethod]
        public void GetParity_Even_ReturnsEvenParity()
        {
            Assert.AreEqual(Parity.Even, SerialPortHelper.GetParity(SerialParity.Even));
        }

        [TestMethod]
        public void GetParity_Mark_ReturnsMarkParity()
        {
            Assert.AreEqual(Parity.Mark, SerialPortHelper.GetParity(SerialParity.Mark));
        }
    }
}
