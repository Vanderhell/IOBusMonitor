using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("ModbusTCPPoint")]
    public class ModbusTCPPoint
    {
        [Key]
        public int Id { get; set; }
        public int ModbusTCPDeviceId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public ICollection<TCPMeasurement> TCPMeasurements { get; set; } = new List<TCPMeasurement>();

        [ForeignKey("ModbusDeviceId")]
        public ModbusTCPDevice ModbusTCPDevice { get; set; }

        [NotMapped]
        public PointType Type => PointType.ModbusTCP;

        public ModbusTCPPoint()
        {

        }

    }
}
