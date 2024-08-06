using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaamerProject.Models
{
    public class Acc_Debentures : Auditable
    {
        public int DebentureId { get; set; }
        public string? DebentureNumber { get; set; }
        public int? Type { get; set; }
        public string? Date { get; set; }
        public string? HijriDate { get; set; }
        public string? Notes { get; set; }
        public int? ServicesId { get; set; }
        public int? FromStorehouseId { get; set; }
        public int? ToStorehouseId { get; set; }
        public int? Qty { get; set; }
        public string? QtyText { get; set; }
        public int? BranchId { get; set; }
        public int? YearId { get; set; }
        public int? CostCenterId { get; set; }

        public virtual Acc_Storehouse? FromStorehouse { get; set; }
        public virtual Acc_Storehouse? ToStorehouse { get; set; }
        public virtual TransactionTypes? TransactionType { get; set; }
        public virtual Acc_Services_Price? Services_Price { get; set; }

    }
}
