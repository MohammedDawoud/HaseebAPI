using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaamerProject.Models
{
    public class Acc_DebenturesVM
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
        public decimal? Qty { get; set; }
        public string? QtyText { get; set; }
        public int? BranchId { get; set; }
        public int? YearId { get; set; }
        public int? CostCenterId { get; set; }

        public string? FromStorehouseStr { get; set; }
        public string? ToStorehouseStr { get; set; }
        public string? TransactionTypeStr { get; set; }
        public string? ServicesNameStr { get; set; }
        public string? ServiceName_EN { get; set; }
        public decimal? Amount { get; set; }
        public int? AccountId { get; set; }
        public decimal? AmountPur { get; set; }
        public int? AccountIdPur { get; set; }
        public int? Begbalance { get; set; }
        public string? SerialNumber { get; set; }
        public string? ItemCode { get; set; }

    }
}
