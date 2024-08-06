using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TaamerProject.Models.Common;

namespace TaamerProject.Service.Interfaces
{
    public interface IAcc_DebenturesService
    {
        Task<IEnumerable<Acc_DebenturesVM>> GetAllDebentures(int Type, int YearId, int BranchId);
        Task<int?> GenerateDebentureNumber(int Type, int BranchId, int? YearId);
        GeneralMessage SaveDebenture(Acc_Debentures Debenture, int UserId, int BranchId, int? YearId);
        GeneralMessage DeleteDebenture(int DebentureId, int UserId, int BranchId);
    }
}
