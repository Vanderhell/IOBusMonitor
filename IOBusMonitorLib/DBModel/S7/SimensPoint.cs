using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("SimensPoint")]
    public class SimensPoint
    {
        [Key]
        public int Id { get; set; }
        public int SimenseDeviceId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public ICollection<SimensMeasurement> SimensMeasurements { get; set; } = new List<SimensMeasurement>();

        [ForeignKey("SimensDeviceId")]
        public SimensDevice SimensDevice { get; set; }

        [NotMapped]
        public PointType Type => PointType.S7;

        public SimensPoint()
        {

        }

    }
}
