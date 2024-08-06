using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haseeb.Models.ViewModels
{
    public class PaymentsAndEarns
    {
        public class DailyPaymentsandEarns
        {
            public string PayType { get; set; }
            public string InvoiceDate { get; set; }
            public string InvoiceValue { get; set; }
            public string TaxAmount { get; set; }
            public string DiscountValue { get; set; }
            public string TotalValue { get; set; }
            public string Cost { get; set; }
            public string Mardod { get; set; }
            public string Earnings { get; set; }
        }
    }
}
