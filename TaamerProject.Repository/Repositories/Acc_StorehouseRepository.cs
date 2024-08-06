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
    public class Acc_StorehouseRepository: IAcc_StorehouseRepository
    {
        private readonly TaamerProjectContext _TaamerProContext;

        public Acc_StorehouseRepository(TaamerProjectContext dataContext)
        {
            _TaamerProContext = dataContext;

        }

        public async Task<IEnumerable<Acc_StorehouseVM>> GetAllStorehouses(string SearchText)
        {
            try
            {
                if (SearchText == "")
                { 
                    var Storehouses = _TaamerProContext.Acc_Storehouse.Where(s => s.IsDeleted == false).Select(x => new Acc_StorehouseVM
                    {
                        StorehouseId = x.StorehouseId,
                        NameAr = x.NameAr ?? "",
                        NameEn = x.NameEn ?? "",
                        Code = x.Code == null ? "" : x.Code,
                        Notes = x.Notes == null ? "" : x.Notes,
                        BranchId = x.BranchId ?? 0,
                    }).ToList();
                    return Storehouses;
                }
                else

                {
                    var Storehouses = _TaamerProContext.Acc_Storehouse.Where(s => s.IsDeleted == false && (s.NameAr.Contains(SearchText) || s.NameEn.Contains(SearchText) || s.Code.Contains(SearchText))).Select(x => new Acc_StorehouseVM
                    {
                        StorehouseId = x.StorehouseId,
                        NameAr = x.NameAr ?? "",
                        NameEn = x.NameEn ?? "",
                        Code = x.Code == null ? "" : x.Code,
                        Notes = x.Notes == null ? "" : x.Notes,
                        BranchId = x.BranchId ?? 0,

                    }).ToList();
                    return Storehouses;
                }
            }
            catch (Exception ex)
            {
                IEnumerable<Acc_StorehouseVM> store = new List<Acc_StorehouseVM>();
                return store;
            }
        }
    }
}
