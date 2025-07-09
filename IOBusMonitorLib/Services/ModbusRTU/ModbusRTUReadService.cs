using EasyModbus;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Reads holding-register data from a Modbus-RTU slave and converts it
    /// into a <see cref="PointViewModel"/>.
    /// </summary>
    public class ModbusRTUReadService : IDisposable
    {
        private ModbusClient _plc = new ModbusClient();

        #region IDisposable
        public void Dispose()
        {
            if (_plc != null && _plc.Connected)
                _plc.Disconnect();
            _plc = null;
        }
        #endregion

        // --------------------------------------------------------------------
        // Low-level register read
        // --------------------------------------------------------------------
        private async Task<object> ReadRegistersAsync(int startAddr,
                                                      int quantity,
                                                      BitOrder order)
        {
            if (!_plc.Connected) return null;

            try
            {
                ushort[] w = await Task.Run(() =>
                    _plc.ReadHoldingRegisters(startAddr, quantity)
                        .Select(x => (ushort)x)
                        .ToArray());

                if (quantity == 1) return w[0];
                if (quantity == 2) return ConvertTwoWordsToFloat(w, order);
                if (quantity == 4) return ConvertFourWordsToDouble(w, order);
                return w;
            }
            catch (Exception ex)
            {
                LogService.LogError("Modbus-RTU register read error: " + ex.Message);
                return null;
            }
        }

        // --------------------------------------------------------------------
        // Public API – read an entire point
        // --------------------------------------------------------------------
        public async Task<PointViewModel> LoadPointDataAsync(ModbusRTUPoint point)
        {
            if (point == null) return new PointViewModel();

            string port = SerialPortHelper.GetSerialPortName(point.ModbusRTUDevice.SerialPort);
            var parity = SerialPortHelper.GetParity(point.ModbusRTUDevice.Parity);
            int baud = (int)point.ModbusRTUDevice.BaudRate;

            _plc = new ModbusClient(port)
            {
                Baudrate = baud,
                Parity = parity,
                UnitIdentifier = (byte)point.ModbusRTUDevice.SlaveId,
                ConnectionTimeout = 2000
            };

            if (!await TryConnectPLCAsync())
                throw new PlcConnectionException(point.ModbusRTUDevice.Name,
                    "Connection failed.");

            var vm = new PointViewModel
            {
                PointId = point.Id,
                DeviceId = point.ModbusRTUDeviceId,
                DeviceName = point.ModbusRTUDevice.Name,
                PointName = point.Name,
                Type = PointType.ModbusRTU,
                Timestamp = DateTime.Now
            };

            foreach (var m in point.RTUMeasurements)
            {
                object raw = await ReadRegistersAsync(m.Register, m.Quantity, m.BitOrder);

                float val = 0f;
                if (raw is ushort u) val = u;
                else if (raw is float f) val = f;
                else if (raw is double d) val = (float)d;

                if (!string.IsNullOrWhiteSpace(m.Condition) &&
                    !m.Condition.Trim().Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    try { val = ConditionEvaluator.Evaluate(m.Condition, val); }
                    catch (Exception ex)
                    {
                        LogService.LogError("Condition evaluation failed: " + ex.Message);
                    }
                }

                double r = Math.Round(val, m.Round);

                vm.Measurements.Add(new MeasurementViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Unit = m.Unit,
                    Value = r,
                    ValueStr = r.ToString("F" + m.Round),
                    Timestamp = DateTime.Now
                });
            }

            return vm;
        }

        // --------------------------------------------------------------------
        // Helpers for 32-/64-bit conversions
        // --------------------------------------------------------------------
        private static float ConvertTwoWordsToFloat(ushort[] w, BitOrder order)
        {
            uint c = order == BitOrder.Swapped
                ? ((uint)w[0] << 16) | w[1]
                : ((uint)w[1] << 16) | w[0];
            return BitConverter.ToSingle(BitConverter.GetBytes(c), 0);
        }

        private static double ConvertFourWordsToDouble(ushort[] w, BitOrder order)
        {
            ulong c = order == BitOrder.Swapped
                ? ((ulong)w[0] << 48) | ((ulong)w[1] << 32) |
                  ((ulong)w[2] << 16) | w[3]
                : ((ulong)w[3] << 48) | ((ulong)w[2] << 32) |
                  ((ulong)w[1] << 16) | w[0];
            return BitConverter.ToDouble(BitConverter.GetBytes(c), 0);
        }

        // --------------------------------------------------------------------
        // Connection helper
        // --------------------------------------------------------------------
        private async Task<bool> TryConnectPLCAsync()
        {
            return await Task.Run(() =>
            {
                try { _plc.Connect(); return _plc.Connected; }
                catch (Exception ex)
                {
                    LogService.LogError("Modbus-RTU connect error: " + ex.Message);
                    return false;
                }
            });
        }
    }
}
