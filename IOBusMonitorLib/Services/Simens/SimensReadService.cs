using S7.Net;
using System;
using System.Collections.ObjectModel;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Reads live data from a Siemens S7 PLC and converts it into
    /// a <see cref="PointViewModel"/> structure.
    /// </summary>
    public class SimensReadService : IDisposable
    {
        private Plc plc;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (plc != null && plc.IsConnected)
                plc.Close();

            plc = null;
        }

        /// <summary>
        /// Opens a TCP connection to the PLC defined in <paramref name="device"/>.
        /// </summary>
        public async Task<bool> TryConnectAsync(SimensDevice device)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var cpu = (CpuType)(int)device.CpuType;
                    plc = new Plc((S7.Net.CpuType)cpu,
                                  device.IPAddress,
                                  (short)device.Rack,
                                  (short)device.Slot);
                    plc.Open();
                    return plc.IsConnected;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Reads a single address and converts the raw value to <c>double</c>.
        /// Returns <c>0.0</c> on communication failure.
        /// </summary>
        public double ReadValue(SimensMeasurement m)
        {
            try
            {
                if (plc == null || !plc.IsConnected) return 0d;

                object raw = plc.Read(m.Address);
                if (raw == null) return 0d;

                switch (m.InferredDataType)
                {
                    case DataType.Bit: return Convert.ToDouble((bool)raw);
                    case DataType.Int: return Convert.ToDouble((short)raw);
                    case DataType.Word: return Convert.ToDouble((ushort)raw);
                    case DataType.Byte: return Convert.ToDouble((byte)raw);
                    case DataType.Double: return Convert.ToDouble(raw);

                    case DataType.Real:
                        // Handle multiple possible underlying formats
                        if (raw is float f) return f;
                        if (raw is double d) return (float)d;
                        if (raw is uint ui) return BitConverter.ToSingle(BitConverter.GetBytes(ui), 0);
                        if (raw is int i) return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
                        if (raw is byte[] b && b.Length == 4) return BitConverter.ToSingle(b, 0);
                        throw new InvalidCastException("Unexpected REAL source type: " + raw.GetType().Name);

                    default:
                        throw new NotSupportedException("Unsupported data type: " + m.InferredDataType);
                }
            }
            catch
            {
                return 0d;
            }
        }

        /// <summary>
        /// Retrieves fresh measurements for an entire <paramref name="point"/>.
        /// </summary>
        public async Task<PointViewModel> LoadPLCActualDataAsync(SimensPoint point)
        {
            if (point == null) return new PointViewModel();

            var device = point.SimensDevice;

            var vm = new PointViewModel
            {
                PointId = point.Id,
                DeviceId = point.SimenseDeviceId,
                DeviceName = device.Name,
                PointName = point.Name,
                Timestamp = DateTime.Now,
                Type = PointType.S7,
                Measurements = new ObservableCollection<MeasurementViewModel>()
            };

            try
            {
                if (!await TryConnectAsync(device))
                    throw new PlcConnectionException(device.Name, $"Connection to PLC '{device.Name}' failed.");

                foreach (var m in point.SimensMeasurements)
                {
                    double value = ReadValue(m);

                    // Optional expression transformation
                    if (m.Condition != "value")
                    {
                        try { value = EvaluateCondition(m.Condition, value); }
                        catch { LogService.LogError($"Condition evaluation failed for '{m.Name}'."); }
                    }

                    double rounded = Math.Round(value, m.Round);
                    vm.Measurements.Add(new MeasurementViewModel
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Unit = m.Unit,
                        Value = rounded,
                        ValueStr = rounded.ToString("F" + m.Round),
                        Timestamp = DateTime.Now
                    });
                }

                return vm;
            }
            catch (Exception ex)
            {
                LogService.LogError("PLC data read error: " + ex.Message);
                throw new PlcConnectionException(device.Name, "Error while reading from Siemens PLC.", ex);
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Evaluates a dynamic C# expression (e.g. <c>"value * 1.8 + 32"</c>)
        /// where the parameter <c>value</c> is the raw measurement value.
        /// </summary>
        public static float EvaluateCondition(string condition, double value)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new InvalidOperationException("Condition string is empty.");

            try
            {
                var param = Expression.Parameter(typeof(float), "value");
                var expr = DynamicExpressionParser.ParseLambda(new[] { param },
                                                                 typeof(float),
                                                                 condition);
                return (float)expr.Compile().DynamicInvoke((float)value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid condition expression.", ex);
            }
        }
    }
}
