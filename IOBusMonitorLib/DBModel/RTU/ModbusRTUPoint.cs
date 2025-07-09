using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("ModbusRTUPoint")]
    public class ModbusRTUPoint
    {
        [Key]
        public int Id { get; set; }
        public int ModbusRTUDeviceId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public ICollection<RTUMeasurement> RTUMeasurements { get; set; } = new List<RTUMeasurement>();

        [ForeignKey("ModbusDeviceId")]
        public ModbusRTUDevice ModbusRTUDevice { get; set; }

        [NotMapped]
        public PointType Type => PointType.ModbusRTU;

        public ModbusRTUPoint()
        {

        }

    }
}
