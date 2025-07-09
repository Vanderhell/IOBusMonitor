using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("ModbusRTUDevice")]
    public class ModbusRTUDevice
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public bool Active { get; set; }
        public SerialPortName SerialPort { get; set; }
        public BaudRate BaudRate { get; set; }
        public SerialParity Parity { get; set; }
        public int SlaveId { get; set; }
        public ICollection<ModbusRTUPoint> ModbusRTUPoints { get; set; } = new List<ModbusRTUPoint>();
    }
}
