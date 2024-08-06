using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TaamerProject.Models.Common;

namespace TaamerProject.Service.Interfaces
{
    public interface IAcc_StorehouseService
    {
        Task<IEnumerable<Acc_StorehouseVM>> GetAllStorehouses(string SearchText);
        GeneralMessage SaveStorehouse(Acc_Storehouse Storehouse, int UserId, int BranchId);
        GeneralMessage DeleteStorehouse(int StorehouseId, int UserId, int BranchId);
    }
}
