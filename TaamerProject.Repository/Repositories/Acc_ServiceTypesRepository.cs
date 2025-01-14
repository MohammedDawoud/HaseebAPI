using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;

namespace TaamerProject.Repository.Repositories
{
    public class Acc_ServiceTypesRepository : IAcc_ServiceTypesRepository
    {
        private readonly TaamerProjectContext _TaamerProContext;
        public Acc_ServiceTypesRepository(TaamerProjectContext dataContext)
        {
            _TaamerProContext = dataContext;
        }

        public async Task<IEnumerable<Acc_ServiceTypesVM>> GetAllServiceTypes()
        {
            var ServiceType = _TaamerProContext.Acc_ServiceTypes.Where(s => s.IsDeleted == false).Select(x => new Acc_ServiceTypesVM
            {
                ServiceTypeId = x.ServiceTypeId,
                NameAr = x.NameAr,
                NameEn = x.NameEn,
            }).ToList();
            return ServiceType;
        }
    }
}
