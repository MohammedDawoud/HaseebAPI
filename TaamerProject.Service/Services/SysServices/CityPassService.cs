﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Repository.Repositories;
using TaamerProject.Service.IGeneric;
using System.Net;
using TaamerProject.Service.Interfaces;
using Haseeb.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class CityPassService : ICityPassService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly ICityPassRepository _CityRepository;



        public CityPassService(TaamerProjectContext dataContext, ISystemAction systemAction, ICityPassRepository cityPassRepository)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _CityRepository = cityPassRepository;
        }


        public async Task<IEnumerable<CityPassVM>> GetAllCities(string SearchText)
        {
            var cities =await _CityRepository.GetAllCities(SearchText);
            return cities;
        }
        public GeneralMessage SaveCity(CityPass city, int UserId, int BranchId)
        {
            try
            {
                if (city.CityId == 0)
                {
                    city.AddUser = UserId;
                    city.AddDate = DateTime.Now;
                    _TaamerProContext.CityPass.Add(city);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة مدينة جديد";
                    _SystemAction.SaveAction("SaveCity", "CityPassService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var CityUpdated = _TaamerProContext.CityPass.Where(x=>x.CityId==city.CityId).FirstOrDefault();
                    if (CityUpdated != null)
                    {
                        CityUpdated.NameAr = city.NameAr;
                        CityUpdated.NameEn = city.NameEn;
                        CityUpdated.Notes = city.Notes;
                        CityUpdated.UpdateUser = UserId;
                        CityUpdated.UpdateDate = DateTime.Now;
                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل مدينة رقم " + city.CityId;
                    _SystemAction.SaveAction("SaveCity", "CityPassService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage {StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_EditedSuccessfully };
                }

            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ المدينة";
                _SystemAction.SaveAction("SaveCity", "CityPassService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage DeleteCity(int CityId, int UserId, int BranchId)
        {
            try
            {
                CityPass city = _TaamerProContext.CityPass.Where(x => x.CityId == CityId).FirstOrDefault();
                city.IsDeleted = true;
                city.DeleteDate = DateTime.Now;
                city.DeleteUser = UserId;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " حذف مدينة رقم " + CityId;
                _SystemAction.SaveAction("DeleteCity", "CityPassService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف مدينة رقم " + CityId; ;
                _SystemAction.SaveAction("DeleteCity", "CityPassService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
        public IEnumerable<object> FillCitySelect(string SearchText)
        {
            return _CityRepository.GetAllCities(SearchText).Result.Select(s => new
            {
                Id = s.CityId,
                Name = s.NameAr
            });
        }

    }
}
