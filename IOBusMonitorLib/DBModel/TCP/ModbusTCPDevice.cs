using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("ModbusTCPDevice")]
    public class ModbusTCPDevice
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public bool Active { get; set; }
        [StringLength(50)]
        public string IPAddress { get; set; }
        public int Port { get; set; }

        public ICollection<ModbusTCPPoint> ModbusTCPPoints { get; set; } = new List<ModbusTCPPoint>();
    }

}
