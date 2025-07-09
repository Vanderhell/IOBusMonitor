using EasyModbus;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Reads holding-registers from a Modbus-TCP device and converts them
    /// into <see cref="PointViewModel"/>. Compatible with C# 7.3.
    /// </summary>
    public class ModbusTCPReadService : IDisposable
    {
        private ModbusClient _plc = new ModbusClient();

        public void Dispose()
        {
            if (_plc != null && _plc.Connected)
                _plc.Disconnect();
            _plc = null;
        }

        // ---------------- low-level read ----------------
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
                LogService.LogError("Modbus-TCP register read error: " + ex.Message);
                return null;
            }
        }

        // ---------------- full point read --------------
        public async Task<PointViewModel> LoadPointDataAsync(ModbusTCPPoint point)
        {
            if (point == null) return new PointViewModel();

            _plc = new ModbusClient(point.ModbusTCPDevice.IPAddress,
                                    point.ModbusTCPDevice.Port)
            { ConnectionTimeout = 2000 };

            if (!await TryConnectPLCAsync())
                throw new PlcConnectionException(point.ModbusTCPDevice.Name,
                    "Connection failed.");

            var vm = new PointViewModel
            {
                PointId = point.Id,
                DeviceId = point.ModbusTCPDeviceId,
                DeviceName = point.ModbusTCPDevice.Name,
                PointName = point.Name,
                Timestamp = DateTime.Now,
                Type = PointType.ModbusTCP,
                Measurements = new ObservableCollection<MeasurementViewModel>()
            };

            foreach (var m in point.TCPMeasurements)
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
                        LogService.LogError("Condition eval failed: " + ex.Message);
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

        // ---------------- helpers ----------------------
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

        private async Task<bool> TryConnectPLCAsync()
        {
            return await Task.Run(() =>
            {
                try { _plc.Connect(); return _plc.Connected; }
                catch (Exception ex)
                {
                    LogService.LogError("Modbus-TCP connect error: " + ex.Message);
                    return false;
                }
            });
        }
    }
}
