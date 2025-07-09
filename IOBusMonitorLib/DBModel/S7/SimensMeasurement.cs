using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IOBusMonitorLib
{
    [Table("SimensMeasurement")]
    public class SimensMeasurement
    {
        [Key]
        public int Id { get; set; }
        public int SimensPointId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public bool Active { get; set; }
        [StringLength(10)]
        public string Unit { get; set; }
        public int Round { get; set; }
        [StringLength(100)]
        public string Condition { get; set; }
        public string Address { get; set; }

        [NotMapped]
        public DataType InferredDataType => SimensAddressHelper.GetDataTypeFromAddress(Address);
        [NotMapped]
        public string InferredDataTypeName => InferredDataType.ToString();

        [ForeignKey("SimensPointId")]
        public SimensPoint SimensPoint { get; set; }
    }
}
