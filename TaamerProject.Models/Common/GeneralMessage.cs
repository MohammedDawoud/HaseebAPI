
using System.Net;

namespace TaamerProject.Models.Common
{
    public class GeneralMessage
    {
        public HttpStatusCode StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public int? ReturnedParm { get; set; }
        public string? ReturnedStr { get; set; }
        public string? ReturnedStrNeeded { get; set; }
        public bool? InvoiceIsDeleted { get; set; }
        public StringContent? Content { get; set; }

        
    }
}
