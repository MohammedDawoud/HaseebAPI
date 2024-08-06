using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;

namespace TaamerProject.Repository.Repositories
{
    public class Acc_DebenturesRepository : IAcc_DebenturesRepository
    {
        private readonly TaamerProjectContext _TaamerProContext;

        public Acc_DebenturesRepository(TaamerProjectContext dataContext)
        {
            _TaamerProContext = dataContext;

        }

        public async Task<IEnumerable<Acc_DebenturesVM>> GetAllDebentures(int Type, int YearId, int BranchId)
        {

            try
            {
                var Debentures = _TaamerProContext.Acc_Debentures.Where(s => s.IsDeleted == false && s.Type== Type && s.YearId== YearId && s.BranchId==BranchId).Select(x => new Acc_DebenturesVM
                {
                    DebentureId = x.DebentureId,
                    DebentureNumber = x.DebentureNumber,
                    Type = x.Type,
                    Date = x.Date,
                    HijriDate = x.HijriDate,
                    Notes = x.Notes??"",
                    ServicesId = x.ServicesId,
                    FromStorehouseId = x.FromStorehouseId,
                    ToStorehouseId = x.ToStorehouseId,
                    Qty = x.Qty??0,
                    QtyText = x.QtyText??"",
                    BranchId = x.BranchId,
                    YearId = x.YearId,
                    CostCenterId = x.CostCenterId,
                    FromStorehouseStr = x.FromStorehouse != null ? x.FromStorehouse.NameAr : "",
                    ToStorehouseStr = x.ToStorehouse != null ? x.ToStorehouse.NameAr : "",
                    TransactionTypeStr = x.TransactionType != null ? x.TransactionType.NameAr : "",
                    ServicesNameStr = x.Services_Price != null ? x.Services_Price.ServicesName : "",
                    ServiceName_EN = x.Services_Price != null ? x.Services_Price.ServiceName_EN : "",
                    Amount = x.Services_Price != null ? x.Services_Price.Amount : 0,
                    AccountId = x.Services_Price != null ? x.Services_Price.AccountId : null,
                    AmountPur = x.Services_Price != null ? x.Services_Price.AmountPur : 0,
                    AccountIdPur = x.Services_Price != null ? x.Services_Price.AccountIdPur : null,
                    Begbalance = x.Services_Price != null ? x.Services_Price.Begbalance : 0,
                    SerialNumber = x.Services_Price != null ? x.Services_Price.SerialNumber : "",
                    ItemCode = x.Services_Price != null ? x.Services_Price.ItemCode : "",

                }).OrderByDescending(s => s.DebentureNumber).ToList(); ;
                return Debentures;
            }
            catch (Exception ex)
            {
                IEnumerable<Acc_DebenturesVM> Debe = new List<Acc_DebenturesVM>();
                return Debe;

            }
        }
        public async Task<int?> GenerateNextDebentureNumber(int Type, int? YearId, int BranchId)
        {
            var invoices = _TaamerProContext.Acc_Debentures.Where(s => s.Type == Type && s.YearId == YearId && s.IsDeleted == false);
            if (invoices != null)
            {
                var lastRow = invoices.OrderByDescending(u => u.DebentureNumber).Take(1).FirstOrDefault();
                if (lastRow != null)
                {
                    var last = Convert.ToInt32(lastRow.DebentureNumber);
                    return last + 1;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }
        }

    }
}
