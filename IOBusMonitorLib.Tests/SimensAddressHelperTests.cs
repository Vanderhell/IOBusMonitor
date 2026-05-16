using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IOBusMonitorLib.Tests
{
    [TestClass]
    public class SimensAddressHelperTests
    {
        [TestMethod]
        public void GetDataTypeFromAddress_DBX_ReturnsBit()
        {
            Assert.AreEqual(DataType.Bit, SimensAddressHelper.GetDataTypeFromAddress("DB1.DBX10.2"));
        }

        [TestMethod]
        public void GetDataTypeFromAddress_DBB_ReturnsByte()
        {
            Assert.AreEqual(DataType.Byte, SimensAddressHelper.GetDataTypeFromAddress("DB1.DBB0"));
        }

        [TestMethod]
        public void GetDataTypeFromAddress_DBW_ReturnsInt()
        {
            Assert.AreEqual(DataType.Int, SimensAddressHelper.GetDataTypeFromAddress("DB1.DBW4"));
        }

        [TestMethod]
        public void GetDataTypeFromAddress_DBD_ReturnsReal()
        {
            Assert.AreEqual(DataType.Real, SimensAddressHelper.GetDataTypeFromAddress("DB1.DBD8"));
        }

        [TestMethod]
        public void GetClrTypeFromAddress_DBX_ReturnsBooleanType()
        {
            Assert.AreEqual(typeof(bool), SimensAddressHelper.GetClrTypeFromAddress("DB1.DBX10.2"));
        }

        [TestMethod]
        public void GetClrTypeFromAddress_UnknownAddress_ReturnsObjectType()
        {
            Assert.AreEqual(typeof(object), SimensAddressHelper.GetClrTypeFromAddress("M10.0"));
        }
    }
}
