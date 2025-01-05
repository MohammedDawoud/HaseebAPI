using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IAcc_ServiceTypesService
    {
        Task<IEnumerable<Acc_ServiceTypesVM>> GetAllServiceTypes();
        GeneralMessage SaveServiceType(Acc_ServiceTypes ServiceType, int UserId, int BranchId);
        GeneralMessage DeleteServiceType(int ServiceTypeId, int UserId, int BranchId);
    }
}
