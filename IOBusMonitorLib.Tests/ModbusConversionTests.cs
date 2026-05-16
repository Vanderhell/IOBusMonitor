using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IOBusMonitorLib.Tests
{
    [TestClass]
    public class ModbusConversionTests
    {
        [TestMethod]
        public void ModbusTcp_ConvertTwoWordsToFloat_NormalOrder_ReturnsExpectedValue()
        {
            ushort[] words = CreateNormalFloatWords(123.5f);
            float result = ModbusTCPReadService.ConvertTwoWordsToFloat(words, BitOrder.Normal);
            Assert.AreEqual(123.5f, result, 0.0001f);
        }

        [TestMethod]
        public void ModbusTcp_ConvertTwoWordsToFloat_SwappedOrder_ReturnsExpectedValue()
        {
            ushort[] words = CreateSwappedFloatWords(123.5f);
            float result = ModbusTCPReadService.ConvertTwoWordsToFloat(words, BitOrder.Swapped);
            Assert.AreEqual(123.5f, result, 0.0001f);
        }

        [TestMethod]
        public void ModbusRtu_ConvertFourWordsToDouble_NormalOrder_ReturnsExpectedValue()
        {
            ushort[] words = CreateNormalDoubleWords(9876.54321d);
            double result = ModbusRTUReadService.ConvertFourWordsToDouble(words, BitOrder.Normal);
            Assert.AreEqual(9876.54321d, result, 0.0000001d);
        }

        [TestMethod]
        public void ModbusRtu_ConvertFourWordsToDouble_SwappedOrder_ReturnsExpectedValue()
        {
            ushort[] words = CreateSwappedDoubleWords(9876.54321d);
            double result = ModbusRTUReadService.ConvertFourWordsToDouble(words, BitOrder.Swapped);
            Assert.AreEqual(9876.54321d, result, 0.0000001d);
        }

        private static ushort[] CreateNormalFloatWords(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            uint bits = BitConverter.ToUInt32(bytes, 0);
            return new[]
            {
                (ushort)(bits & 0xFFFF),
                (ushort)((bits >> 16) & 0xFFFF)
            };
        }

        private static ushort[] CreateSwappedFloatWords(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            uint bits = BitConverter.ToUInt32(bytes, 0);
            return new[]
            {
                (ushort)((bits >> 16) & 0xFFFF),
                (ushort)(bits & 0xFFFF)
            };
        }

        private static ushort[] CreateNormalDoubleWords(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            ulong bits = BitConverter.ToUInt64(bytes, 0);
            return new[]
            {
                (ushort)(bits & 0xFFFF),
                (ushort)((bits >> 16) & 0xFFFF),
                (ushort)((bits >> 32) & 0xFFFF),
                (ushort)((bits >> 48) & 0xFFFF)
            };
        }

        private static ushort[] CreateSwappedDoubleWords(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            ulong bits = BitConverter.ToUInt64(bytes, 0);
            return new[]
            {
                (ushort)((bits >> 48) & 0xFFFF),
                (ushort)((bits >> 32) & 0xFFFF),
                (ushort)((bits >> 16) & 0xFFFF),
                (ushort)(bits & 0xFFFF)
            };
        }
    }
}
