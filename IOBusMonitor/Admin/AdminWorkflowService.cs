using EasyModbus;
using IOBusMonitorLib;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IOBusMonitor
{
    internal static class AdminWorkflowService
    {
        private static readonly Regex SiemensByteAddressPattern =
            new Regex(@"^DB\d+\.DB(B|W|D|L)\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SiemensBitAddressPattern =
            new Regex(@"^DB\d+\.DBX\d+\.[0-7]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Validate(ModbusTCPDevice device, IEnumerable<ModbusTCPDevice> devices)
        {
            if (device == null)
                return "Device row is missing.";
            if (string.IsNullOrWhiteSpace(device.Name))
                return "Device name is required.";
            if (!IsValidHost(device.IPAddress))
                return "IP address or host name is invalid.";
            if (!IsValidPort(device.Port))
                return "Port must be between 1 and 65535.";

            string name = Normalize(device.Name);
            string endpoint = Normalize(device.IPAddress) + ":" + device.Port;

            if (devices.Any(d => !ReferenceEquals(d, device) && Normalize(d.Name) == name))
                return "Device name must be unique.";
            if (devices.Any(d => !ReferenceEquals(d, device) &&
                                 Normalize(d.IPAddress) + ":" + d.Port == endpoint))
                return "Device endpoint must be unique.";

            return null;
        }

        public static string Validate(ModbusRTUDevice device, IEnumerable<ModbusRTUDevice> devices)
        {
            if (device == null)
                return "Device row is missing.";
            if (string.IsNullOrWhiteSpace(device.Name))
                return "Device name is required.";
            if (!Enum.IsDefined(typeof(SerialPortName), device.SerialPort))
                return "Serial port is invalid.";
            if (!Enum.IsDefined(typeof(BaudRate), device.BaudRate))
                return "Baud rate is invalid.";
            if (!Enum.IsDefined(typeof(SerialParity), device.Parity))
                return "Parity is invalid.";
            if (device.SlaveId < 1 || device.SlaveId > 247)
                return "Slave ID must be between 1 and 247.";

            string name = Normalize(device.Name);
            string endpoint = device.SerialPort + ":" + device.SlaveId;

            if (devices.Any(d => !ReferenceEquals(d, device) && Normalize(d.Name) == name))
                return "Device name must be unique.";
            if (devices.Any(d => !ReferenceEquals(d, device) &&
                                 d.SerialPort == device.SerialPort &&
                                 d.SlaveId == device.SlaveId))
                return "Serial port + slave ID must be unique.";

            return null;
        }

        public static string Validate(SimensDevice device, IEnumerable<SimensDevice> devices)
        {
            if (device == null)
                return "Device row is missing.";
            if (string.IsNullOrWhiteSpace(device.Name))
                return "Device name is required.";
            if (!IsValidHost(device.IPAddress))
                return "IP address or host name is invalid.";
            if (!IsValidPort(device.Port))
                return "Port must be between 1 and 65535.";
            if (device.Rack < 0 || device.Rack > 7)
                return "Rack must be between 0 and 7.";
            if (device.Slot < 0 || device.Slot > 31)
                return "Slot must be between 0 and 31.";
            if (!Enum.IsDefined(typeof(CpuType), device.CpuType))
                return "CPU type is invalid.";

            string name = Normalize(device.Name);
            string endpoint = Normalize(device.IPAddress) + ":" + device.Port + ":" + device.Rack + ":" + device.Slot;

            if (devices.Any(d => !ReferenceEquals(d, device) && Normalize(d.Name) == name))
                return "Device name must be unique.";
            if (devices.Any(d => !ReferenceEquals(d, device) &&
                                 Normalize(d.IPAddress) + ":" + d.Port + ":" + d.Rack + ":" + d.Slot == endpoint))
                return "Device endpoint must be unique.";

            return null;
        }

        public static string Validate(ModbusTCPPoint point, IEnumerable<ModbusTCPPoint> points, IEnumerable<ModbusTCPDevice> devices)
        {
            if (point == null)
                return "Point row is missing.";
            if (string.IsNullOrWhiteSpace(point.Name))
                return "Point name is required.";
            if (!devices.Any(d => d.Id == point.ModbusTCPDeviceId))
                return "Select a valid device.";

            string name = Normalize(point.Name);
            if (points.Any(p => !ReferenceEquals(p, point) &&
                                p.ModbusTCPDeviceId == point.ModbusTCPDeviceId &&
                                Normalize(p.Name) == name))
                return "Point name must be unique per device.";

            return null;
        }

        public static string Validate(ModbusRTUPoint point, IEnumerable<ModbusRTUPoint> points, IEnumerable<ModbusRTUDevice> devices)
        {
            if (point == null)
                return "Point row is missing.";
            if (string.IsNullOrWhiteSpace(point.Name))
                return "Point name is required.";
            if (!devices.Any(d => d.Id == point.ModbusRTUDeviceId))
                return "Select a valid device.";

            string name = Normalize(point.Name);
            if (points.Any(p => !ReferenceEquals(p, point) &&
                                p.ModbusRTUDeviceId == point.ModbusRTUDeviceId &&
                                Normalize(p.Name) == name))
                return "Point name must be unique per device.";

            return null;
        }

        public static string Validate(SimensPoint point, IEnumerable<SimensPoint> points, IEnumerable<SimensDevice> devices)
        {
            if (point == null)
                return "Point row is missing.";
            if (string.IsNullOrWhiteSpace(point.Name))
                return "Point name is required.";
            if (!devices.Any(d => d.Id == point.SimenseDeviceId))
                return "Select a valid device.";

            string name = Normalize(point.Name);
            if (points.Any(p => !ReferenceEquals(p, point) &&
                                p.SimenseDeviceId == point.SimenseDeviceId &&
                                Normalize(p.Name) == name))
                return "Point name must be unique per device.";

            return null;
        }

        public static string Validate(TCPMeasurement measurement, IEnumerable<TCPMeasurement> measurements, IEnumerable<ModbusTCPPoint> points)
        {
            if (measurement == null)
                return "Measurement row is missing.";
            if (string.IsNullOrWhiteSpace(measurement.Name))
                return "Measurement name is required.";
            if (string.IsNullOrWhiteSpace(measurement.Condition))
                return "Condition is required. Use 'value' for pass-through.";
            if (!points.Any(p => p.Id == measurement.ModbusTCPPointId))
                return "Select a valid point.";
            if (measurement.Register < 0)
                return "Register must be zero or greater.";
            if (!IsSupportedModbusQuantity(measurement.Quantity))
                return "Quantity must be 1, 2, or 4.";
            if (measurement.Round < 0 || measurement.Round > 6)
                return "Rounding must be between 0 and 6.";

            string formulaError = ValidateFormula(measurement.Condition);
            if (formulaError != null)
                return formulaError;

            string name = Normalize(measurement.Name);
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.ModbusTCPPointId == measurement.ModbusTCPPointId &&
                                      Normalize(m.Name) == name))
                return "Measurement name must be unique per point.";
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.ModbusTCPPointId == measurement.ModbusTCPPointId &&
                                      m.Register == measurement.Register &&
                                      m.Quantity == measurement.Quantity))
                return "Register + quantity must be unique per point.";

            return null;
        }

        public static string Validate(RTUMeasurement measurement, IEnumerable<RTUMeasurement> measurements, IEnumerable<ModbusRTUPoint> points)
        {
            if (measurement == null)
                return "Measurement row is missing.";
            if (string.IsNullOrWhiteSpace(measurement.Name))
                return "Measurement name is required.";
            if (string.IsNullOrWhiteSpace(measurement.Condition))
                return "Condition is required. Use 'value' for pass-through.";
            if (!points.Any(p => p.Id == measurement.ModbusRTUPointId))
                return "Select a valid point.";
            if (measurement.Register < 0)
                return "Register must be zero or greater.";
            if (!IsSupportedModbusQuantity(measurement.Quantity))
                return "Quantity must be 1, 2, or 4.";
            if (measurement.Round < 0 || measurement.Round > 6)
                return "Rounding must be between 0 and 6.";

            string formulaError = ValidateFormula(measurement.Condition);
            if (formulaError != null)
                return formulaError;

            string name = Normalize(measurement.Name);
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.ModbusRTUPointId == measurement.ModbusRTUPointId &&
                                      Normalize(m.Name) == name))
                return "Measurement name must be unique per point.";
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.ModbusRTUPointId == measurement.ModbusRTUPointId &&
                                      m.Register == measurement.Register &&
                                      m.Quantity == measurement.Quantity))
                return "Register + quantity must be unique per point.";

            return null;
        }

        public static string Validate(SimensMeasurement measurement, IEnumerable<SimensMeasurement> measurements, IEnumerable<SimensPoint> points)
        {
            if (measurement == null)
                return "Measurement row is missing.";
            if (string.IsNullOrWhiteSpace(measurement.Name))
                return "Measurement name is required.";
            if (string.IsNullOrWhiteSpace(measurement.Condition))
                return "Condition is required. Use 'value' for pass-through.";
            if (!points.Any(p => p.Id == measurement.SimensPointId))
                return "Select a valid point.";
            if (measurement.Round < 0 || measurement.Round > 6)
                return "Rounding must be between 0 and 6.";
            if (!IsValidSiemensAddress(measurement.Address))
                return "Siemens address must look like DB1.DBX0.0, DB1.DBB0, DB1.DBW2, DB1.DBD4, or DB1.DBL8.";

            string formulaError = ValidateFormula(measurement.Condition);
            if (formulaError != null)
                return formulaError;

            string name = Normalize(measurement.Name);
            string address = Normalize(measurement.Address);
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.SimensPointId == measurement.SimensPointId &&
                                      Normalize(m.Name) == name))
                return "Measurement name must be unique per point.";
            if (measurements.Any(m => !ReferenceEquals(m, measurement) &&
                                      m.SimensPointId == measurement.SimensPointId &&
                                      Normalize(m.Address) == address))
                return "Address must be unique per point.";

            return null;
        }

        public static async Task<string> TestConnectionAsync(ModbusTCPDevice device)
        {
            string validation = Validate(device, new[] { device });
            if (validation != null)
                return validation;

            var client = new ModbusClient(device.IPAddress, device.Port)
            {
                ConnectionTimeout = 2000
            };

            try
            {
                await Task.Run(() => client.Connect());
                return client.Connected
                    ? "Connection OK."
                    : "Connection failed.";
            }
            catch (Exception ex)
            {
                return "Connection failed: " + ex.Message;
            }
            finally
            {
                if (client.Connected)
                    client.Disconnect();
            }
        }

        public static string TestSerialPort(ModbusRTUDevice device)
        {
            string validation = Validate(device, new[] { device });
            if (validation != null)
                return validation;

            string portName = SerialPortHelper.GetSerialPortName(device.SerialPort);
            try
            {
                var knownPorts = SerialPort.GetPortNames();
                if (!knownPorts.Any(p => string.Equals(p, portName, StringComparison.OrdinalIgnoreCase)))
                    return "Serial port is not present on this machine.";

                using (var port = new SerialPort(portName, (int)device.BaudRate, SerialPortHelper.GetParity(device.Parity), 8, StopBits.One))
                {
                    port.Open();
                    bool open = port.IsOpen;
                    port.Close();
                    return open
                        ? "Serial port is available."
                        : "Serial port could not be opened.";
                }
            }
            catch (Exception ex)
            {
                return "Serial port test failed: " + ex.Message;
            }
        }

        public static async Task<string> TestConnectionAsync(SimensDevice device)
        {
            string validation = Validate(device, new[] { device });
            if (validation != null)
                return validation;

            using (var reader = new SimensReadService())
            {
                try
                {
                    bool connected = await reader.TryConnectAsync(device);
                    return connected
                        ? "Connection OK."
                        : "Connection failed.";
                }
                catch (Exception ex)
                {
                    return "Connection failed: " + ex.Message;
                }
            }
        }

        public static string TestFormula(string condition)
        {
            string validation = ValidateFormula(condition);
            if (validation != null)
                return validation;

            try
            {
                float sampleInput = 12.34f;
                float sampleOutput = ConditionEvaluator.Evaluate(condition, sampleInput);
                return "Formula OK. Sample value 12.34 -> " + sampleOutput.ToString("0.###");
            }
            catch (Exception ex)
            {
                return "Formula test failed: " + ex.Message;
            }
        }

        private static string ValidateFormula(string condition)
        {
            try
            {
                ConditionEvaluator.Evaluate(condition, 12.34f);
                return null;
            }
            catch (Exception ex)
            {
                return "Invalid condition expression: " + ex.Message;
            }
        }

        private static bool IsValidHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
                return true;

            var hostNameType = Uri.CheckHostName(host.Trim());
            return hostNameType == UriHostNameType.Dns || hostNameType == UriHostNameType.IPv6;
        }

        private static bool IsValidPort(int port)
        {
            return port >= 1 && port <= 65535;
        }

        private static bool IsSupportedModbusQuantity(int quantity)
        {
            return quantity == 1 || quantity == 2 || quantity == 4;
        }

        private static bool IsValidSiemensAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            if (SimensAddressHelper.GetDataTypeFromAddress(address) == DataType.Unknown)
                return false;

            return SiemensBitAddressPattern.IsMatch(address.Trim()) ||
                   SiemensByteAddressPattern.IsMatch(address.Trim());
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
