using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haseeb.Models.ViewModels
{
    public class DelegatesVM
    {
        public List<Invoice>? InvoicePaid { get; set; }
        public List<Invoice>? InvoiceMardod { get; set; }
        public decimal? TotalPaid { get; set; }
        public decimal? DiscountPaid { get; set; }
        public decimal? TotalPaidafterdiscount { get; set; }
        public decimal? TotalPaidEarnings { get; set; }
        public decimal? TotalMardod { get; set; }
        public decimal? DiscountMardod { get; set; }
        public decimal? TotalMardodafterdiscount { get; set; }
        public decimal? TotalMardodEarnings { get; set; }
        public decimal? SumTotal { get; set; }
        public decimal? SumDiscount { get; set; }
        public decimal? SumTotalafterdiscount { get; set; }
        public decimal? SumEarnings { get; set; }



    }
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? PayType { get; set; }
        public string? Date { get; set; }
        public string? Notes { get; set; }
        public decimal? InvoiceValue { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? TotalValue { get; set; }
        public decimal? PaidValue { get; set; }
        public decimal? Remaining { get; set; }
        public decimal? TotalPurches { get; set; }

    }
}
