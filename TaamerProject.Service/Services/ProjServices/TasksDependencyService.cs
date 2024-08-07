using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Service.IGeneric;
using System.Net;
using TaamerProject.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using TaamerProject.Service.Generic;
using Haseeb.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class TasksDependencyService :  ITasksDependencyService
    {
        private readonly ITasksDependencyRepository _TasksDependencyRepository;
        private readonly IProjectPhasesTasksRepository _ProjectPhasesTasksRepository;
        private readonly IProjectRepository _ProjectRepository;
        private readonly INodeLocationsRepository _NodeLocationsRepository;
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        public TasksDependencyService(TaamerProjectContext dataContext, ISystemAction systemAction, 
            ITasksDependencyRepository tasksDependencyRepository, IProjectPhasesTasksRepository projectPhasesTasksRepository,
            IProjectRepository projectRepository, INodeLocationsRepository  nodeLocationsRepository)
        {
            _TasksDependencyRepository = tasksDependencyRepository;
            _ProjectPhasesTasksRepository = projectPhasesTasksRepository;
            _ProjectRepository = projectRepository;
            _NodeLocationsRepository = nodeLocationsRepository;
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
        }
        public Task<IEnumerable<TasksDependencyVM>> GetAllTasksDependencies(int BranchId)
        {
            var Dependencies = _TasksDependencyRepository.GetAllTasksDependencies(BranchId);
            return Dependencies;
        }
        public GeneralMessage SaveTasksDependency(TasksDependency TasksDependency, int UserId, int BranchId)
        {
            try
            {
                if (TasksDependency.DependencyId == 0)
                {
                    TasksDependency.AddUser = UserId;
                    TasksDependency.BranchId = BranchId;
                    TasksDependency.AddDate = DateTime.Now;
                    _TaamerProContext.TasksDependency.Add(TasksDependency);
                     _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سير مهام جديد";
                    _SystemAction.SaveAction("SaveTasksDependency", "TasksDependencyService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                   // var TasksDependencyUpdated = _TasksDependencyRepository.GetById(TasksDependency.DependencyId);
                    TasksDependency? TasksDependencyUpdated =  _TaamerProContext.TasksDependency.Where(s => s.DependencyId == TasksDependency.DependencyId).FirstOrDefault();

                    if (TasksDependencyUpdated != null)
                    {
                        TasksDependencyUpdated.PredecessorId = TasksDependency.PredecessorId;
                        TasksDependencyUpdated.SuccessorId = TasksDependency.SuccessorId;
                        TasksDependencyUpdated.ProjSubTypeId = TasksDependency.ProjSubTypeId;
                        TasksDependencyUpdated.Type = TasksDependency.Type;
                        TasksDependencyUpdated.BranchId = TasksDependency.BranchId;
                        TasksDependencyUpdated.UpdateUser = UserId;
                        TasksDependencyUpdated.UpdateDate = DateTime.Now;
                    }
                     _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سير مهام رقم " + TasksDependency.DependencyId;
                    _SystemAction.SaveAction("SaveTasksDependency", "TasksDependencyService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception)
            { //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ سير المهام";
                _SystemAction.SaveAction("SaveTasksDependency", "TasksDependencyService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveDependencyPhasesTask(int ProjectId, List<TasksDependency> TaskLink,List<NodeLocations> NodeLocList, int UserId, int BranchId)
        {
            try
            {
               // var existingDependencySettings = _TasksDependencyRepository.GetMatching(s => s.IsDeleted == false && s.ProjectId == ProjectId);
                var existingDependencySettings = _TaamerProContext.TasksDependency.Where(s => s.IsDeleted == false && s.ProjectId == ProjectId).ToList();
                if (existingDependencySettings.Count() > 0)
                {
                    _TaamerProContext.TasksDependency.RemoveRange(existingDependencySettings);

                }

                if (TaskLink != null)
                {
                    foreach (var item in TaskLink)
                    {
                        if (item.PredecessorId != null || item.SuccessorId != null)
                        {
                            var dependncy = new TasksDependency();
                            dependncy.PredecessorId = item.PredecessorId;
                            dependncy.SuccessorId = item.SuccessorId;
                            dependncy.Type = 0;
                            dependncy.ProjectId = ProjectId;


                            // dependncy.ProjSubTypeId = _ProjectRepository.GetById(ProjectId).SubProjectTypeId;
                            var ProjSubTypeId =  _TaamerProContext.Project.Where(s => s.ProjectId == ProjectId).FirstOrDefault()!.SubProjectTypeId??0;
                            dependncy.ProjSubTypeId = ProjSubTypeId;

                            dependncy.IsDeleted = false;
                            dependncy.AddDate = DateTime.Now;
                            dependncy.BranchId = BranchId;
                            dependncy.AddUser = UserId;
                            _TaamerProContext.TasksDependency.Add(dependncy);
                        }
                    }

                }
                //var existingNodeLocation = _NodeLocationsRepository.GetMatching(s => s.ProjectId == ProjectId);
                var existingNodeLocation = _TaamerProContext.NodeLocations.Where(s => s.ProjectId == ProjectId).ToList();

                if (existingNodeLocation.Count()>0)
                {
                    _TaamerProContext.NodeLocations.RemoveRange(existingNodeLocation);
                }

                if (NodeLocList != null)
                {
                    foreach (var item in NodeLocList)
                    {

                       // var SettingPhase = _ProjectPhasesTasksRepository.GetMatching(s => s.PhaseTaskId == item.TaskId && s.Type != 3 && s.ProjectId == ProjectId && s.IsDeleted==false).Select(x => x.PhaseTaskId);
                        var SettingPhase = _TaamerProContext.ProjectPhasesTasks.Where(s => s.PhaseTaskId == item.TaskId && s.Type != 3 && s.ProjectId == ProjectId && s.IsDeleted == false).Select(x => x.PhaseTaskId);

                        foreach (var i in SettingPhase)
                        {
                            //var count = _ProjectPhasesTasksRepository.GetMatching(s => s.Type == 3 && s.ProjectId == ProjectId && s.ParentId == i && s.IsDeleted==false).Count();
                            var count = _TaamerProContext.ProjectPhasesTasks.Where(s=>s.Type ==3 && s.ProjectId == ProjectId && s.ParentId == i && s.IsDeleted == false).Count();
                            if (count == 0)
                            { 
                                //-----------------------------------------------------------------------------------------------------------------
                                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                                string ActionNote = "فشل في حفظ سير مهام";
                                _SystemAction.SaveAction("SaveDependencyPhasesTask", "TasksDependencyService", 1, Resources.CanNotSaveTheStepsWithOutTasks, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                                //-----------------------------------------------------------------------------------------------------------------

                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.CanNotSaveTheStepsWithOutTasks };
                            }
                        }


                        var Loc = new NodeLocations();
                        Loc.ProjectId = ProjectId;
                        Loc.TaskId = item.TaskId;
                        Loc.Location = item.Location;
                        Loc.AddDate = DateTime.Now;
                        Loc.AddUser = UserId;
                        _TaamerProContext.NodeLocations.Add(item);
                         _TaamerProContext.SaveChanges();

                       // var relatedTaks = _ProjectPhasesTasksRepository.GetById(item.TaskId);
                        var relatedTaks = _TaamerProContext.ProjectPhasesTasks.Where(s => s.PhaseTaskId == item.TaskId).FirstOrDefault();
                        if (relatedTaks != null)
                        {
                            relatedTaks.LocationId = item.LocationId;
                        }
                       
                    }
                }
                else
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ سير مهام";
                    _SystemAction.SaveAction("SaveDependencyPhasesTask", "TasksDependencyService", 1, Resources.CanNotSaveTheStepsWithOutTasks, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.CanNotSaveTheStepsWithOutTasks };
                }

                 _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = "اضافة سير مهام جديد";
                _SystemAction.SaveAction("SaveDependencyPhasesTask", "TasksDependencyService", 1, Resources.TasksSavedSuccessfully, "", "", ActionDate2, UserId, BranchId, ActionNote2, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.TasksSavedSuccessfully};
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ سير مهام";
                _SystemAction.SaveAction("SaveDependencyPhasesTask", "TasksDependencyService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage DeleteTasksDependency(int DependencyId, int UserId, int BranchId)
        {
            try
            {
               // TasksDependency TasksDependency = _TasksDependencyRepository.GetById(DependencyId);
                TasksDependency? TasksDependency =   _TaamerProContext.TasksDependency.Where(s => s.DependencyId == DependencyId).FirstOrDefault();
                if (TasksDependency != null)
                {
                    TasksDependency.IsDeleted = true;
                    TasksDependency.DeleteDate = DateTime.Now;
                    TasksDependency.DeleteUser = UserId;
                    _TaamerProContext.SaveChanges();
                }
               
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " حذف سير مهام رقم " + DependencyId;
                _SystemAction.SaveAction("DeleteTasksDependency", "TasksDependencyService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            { 
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف سير مهام رقم " + DependencyId; ;
                _SystemAction.SaveAction("DeleteTasksDependency", "TasksDependencyService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------


                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
        public ProjectTasksNodeVM GetTasksNodeByProjectId(int ProjectId)
        {
            var NodeTasks = new ProjectTasksNodeVM();
            NodeTasks.nodeDataArray = _ProjectPhasesTasksRepository.GetAllPhasesTasksByProjectId(ProjectId).Result;
            NodeTasks.linkDataArray = _TasksDependencyRepository.GetAllDependencyByProjectId(ProjectId).Result;
            //var succesorIds = _TasksDependencyRepository.GetMatching(s => s.IsDeleted == false && s.ProjectId == ProjectId && (s.PredecessorId !=0 && s.SuccessorId!=0)).Select(s => s.SuccessorId);
            var succesorIds = _TaamerProContext.TasksDependency.Where(s => s.IsDeleted == false && s.ProjectId == ProjectId && (s.PredecessorId != 0 && s.SuccessorId != 0)).Select(s => s.SuccessorId);
           // var predecessorIds = _TasksDependencyRepository.GetMatching(s => s.IsDeleted == false && s.ProjectId == ProjectId && (s.PredecessorId != 0 && s.SuccessorId != 0)).Select(s => s.PredecessorId);
            var predecessorIds = _TaamerProContext.TasksDependency.Where(s => s.IsDeleted == false && s.ProjectId == ProjectId && (s.PredecessorId != 0 && s.SuccessorId != 0)).Select(s => s.PredecessorId);

            NodeTasks.firstLevelNode = predecessorIds.Except(succesorIds).Select(s => new ProjectPhasesTasksVM
            {
                PhaseTaskId = s.Value,
            });
            return NodeTasks;
        }

        public TasksDependency GetTasksDependency(int ProjectId)
        {

            var ProjectIds = _TaamerProContext.TasksDependency.Where(s => s.IsDeleted == false && s.ProjectId == ProjectId).FirstOrDefault();

            return ProjectIds??new TasksDependency();
        }

        public List<AccountTreeVM> GetProjectPhasesTaskTree(int ProjectId)
        {
            var ProPha = _TaamerProContext.ProjectPhasesTasks.Where(s => s.IsDeleted == false && s.ProjectId == ProjectId).OrderBy(s => s.PhaseTaskId).ToList();

            if (ProPha != null && ProPha.Count() > 0)
            {
                List<AccountTreeVM> treeItems = new List<AccountTreeVM>();
                foreach (var item in ProPha)
                {
                    treeItems.Add(new AccountTreeVM(item.PhaseTaskId.ToString(), ((item.ParentId == 0 || item.ParentId == null) ? "#" : item.ParentId.ToString()), item.DescriptionAr = item.DescriptionAr));
                }
                return treeItems;
            }
            else
            {
                return new List<AccountTreeVM>();
            }
        }


       
    }
}
