
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("RTUMeasurement")]
    public class RTUMeasurement
    {
        [Key]
        public int Id { get; set; }

        public int ModbusRTUPointId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        [StringLength(10)]
        public string Unit { get; set; }
        public int Round { get; set; }
        [StringLength(100)]
        public string Condition { get; set; }
        public int Register { get; set; }
        public int Quantity { get; set; }
        public bool Active { get; set; }
        public BitOrder BitOrder { get; set; }


        [ForeignKey("ModbusRTUPointId")]
        public ModbusRTUPoint ModbusRTUPoint { get; set; }
    }


}


