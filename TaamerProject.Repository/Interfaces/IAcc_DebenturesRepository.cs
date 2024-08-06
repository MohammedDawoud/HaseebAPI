using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaamerProject.Repository.Interfaces
{
    public interface IAcc_DebenturesRepository
    {
        Task<IEnumerable<Acc_DebenturesVM>> GetAllDebentures(int Type,int YearId, int BranchId);
        Task<int?> GenerateNextDebentureNumber(int Type, int? YearId, int BranchId);


    }
}
