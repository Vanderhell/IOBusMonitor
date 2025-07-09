using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("SimensDevice")]
    public class SimensDevice
    {

        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public bool Active { get; set; }
        [StringLength(50)]
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public CpuType CpuType { get; set; }

        public ICollection<SimensPoint> SimensPoints { get; set; } = new List<SimensPoint>();

    }
}
