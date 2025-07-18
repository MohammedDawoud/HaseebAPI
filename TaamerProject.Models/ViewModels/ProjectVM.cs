﻿using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaamerProject.Models
{
    
    public class ProjectVM
    {
        public int Id { get; set; }
        public string? Name{ get; set; }
        public int ProjectId { get; set; }
        public int? CustomerId { get; set; }
        public int? ParentProjectId { get; set; }
        public int? TransactionTypeId { get; set; }
        public int? MangerId { get; set; }
        public string? NationalNumber { get; set; }
        public string? Mobile { get; set; }
        public string? ProjectDate { get; set; }
        public string? ProjectHijriDate { get; set; }
        public string? ProjectExpireDate { get; set; }
        public string? ProjectExpireHijriDate { get; set; }
        public string? SiteName { get; set; }
        public int? ProjectTypeId { get; set; }
        public int? SubProjectTypeId { get; set; }
        public string? OrderType { get; set; }
        public string? SketchName { get; set; }
        public string? SketchNo { get; set; }
        public int? PieceNo { get; set; }
        public int? AdwAR { get; set; }
        public int? Status { get; set; }
        public string? OrderNo { get; set; }
        public string? OutBoxNo { get; set; }
        public string? OutBoxDate { get; set; }
        public string? OutBoxHijriDate { get; set; }
        public string? Reason1 { get; set; }
        public string? Notes1 { get; set; }
        public string? Subject { get; set; }
        public string? XPoint { get; set; }
        public string? YPoint { get; set; }
        public string? Technical { get; set; }
        public string? Prosedor { get; set; }
        public string? ReasonRevers { get; set; }
        public string? EngNotes { get; set; }
        public string? ReverseDate { get; set; }
        public string? ReverseHijriDate { get; set; }
        public int? OrderStatus { get; set; }
        public int? UserId { get; set; }
        public int? Receipt { get; set; }
        public bool? PayStatus { get; set; }
        public string? RegionName { get; set; }
        public string? DistrictName { get; set; }
        public string? SiteType { get; set; }
        public string? ContractNo { get; set; }
        public string? ContractDate { get; set; }
        public string? ContractHijriDate { get; set; }
        public string? ContractSource { get; set; }
        public string? SiteNo { get; set; }
        public string? PayanNo { get; set; }
        public int? JehaId { get; set; }
        public string? ZaraaSak { get; set; }
        public string? ZaraaNatural { get; set; }
        public string? BordersSak { get; set; }
        public string? BordersNatural { get; set; }
        public string? Ertedad { get; set; }
        public string? Brooz { get; set; }
        public string? AreaSak { get; set; }
        public string? AreaNatural { get; set; }
        public string? AreaArrange { get; set; }
        public int? BuildingType { get; set; }
        public string? BuildingPercent { get; set; }
        public string? SpaceName { get; set; }
        public string? Office { get; set; }
        public string? Usage { get; set; }
        public string? Docpath { get; set; }
        public int? RegionTypeId { get; set; }
        public string? elevators { get; set; }
        public string? typ1 { get; set; }
        public string? brozat { get; set; }
        public string? entries { get; set; }
        public string? Basement { get; set; }
        public string? GroundFloor { get; set; }
        public string? FirstFloor { get; set; }
        public string? Motkrr { get; set; }
        public string? FirstExtension { get; set; }
        public string? ExtensionName { get; set; }
        public string? GeneralLocation { get; set; }
        public string? LicenseNo { get; set; }
        public string? Licensedate { get; set; }
        public string? LicenseHijridate { get; set; }
        public string? DesiningOffice { get; set; }
        public int? estsharyformoslhat { get; set; }
        public int? Consultantfinishing { get; set; }
        public string? Period { get; set; }
        public int? punshmentamount { get; set; }
        public decimal? FirstPay { get; set; }
        public string? LicenseContent { get; set; }
        public int? OtherStatus { get; set; }
        public string? AreaSpace { get; set; }
        public string? ContractorName { get; set; }
        public string? ContractorMobile { get; set; }
        public string? SupervisionSatartDate { get; set; }
        public string? SupervisionSatartHijriDate { get; set; }
        public string? SupervisionEndDate { get; set; }
        public string? SupervisionEndHijriDate { get; set; }
        public string? SupervisionNo { get; set; }
        public string? SupervisionNotes { get; set; }
        public string? qaboqwaedmostlm { get; set; }
        public string? qaboreqabmostlm { get; set; }
        public string? qabosaqfmostlm { get; set; }
        public string? molhqalwisaqffash { get; set; }
        public string? molhqalwisaqfdate { get; set; }
        public string? molhqalwisaqfHijridate { get; set; }
        public string? molhqalwisaqfmostlm { get; set; }
        public string? molhqardisaqffash { get; set; }
        public string? molhqardisaqfdate { get; set; }
        public string? molhqardisaqfHijridate { get; set; }
        public string? molhqardisaqfmostlm { get; set; }
        public int? FinalOrder { get; set; }
        public string? SpaceBuild { get; set; }
        public string? FloorEstablishing { get; set; }
        public string? Roof { get; set; }
        public string? Electric { get; set; }
        public string? Takeef { get; set; }
        public string? ProjectNo { get; set; }
        public string? LimitDate { get; set; }
        public string? LimitHijriDate { get; set; }
        public int? LimitDays { get; set; }
        public string? NoteDate { get; set; }
        public string? NoteHijriDate { get; set; }
        public string? ResponseEng { get; set; }
        public int? ReseveStatus { get; set; }
        public string? kaeedno { get; set; }
        public string? TechnicalDemands { get; set; }
        public string? Todoaction { get; set; }
        public string? Responsible { get; set; }
        public int? ExternalEmpId { get; set; }
        public string? FinishDate { get; set; }
        public string? FinishHijriDate { get; set; }
        public int? ContractPeriod { get; set; }
        public string? SpaceNotes { get; set; }
        public string? ContractNotes { get; set; }
        public int? SpaceId { get; set; }
        public int? CityId { get; set; }
        public string? ProjectDescription { get; set; }
        public int? Paied { get; set; }
        public int? Discount { get; set; }
        public int? Fees { get; set; }
        public string? ProjectTypeName { get; set; }
        public string? ProjectRegionName { get; set; }
        public string? Catego { get; set; }
        public string? ContractPeriodType { get; set; }
        public int? ContractPeriodMinites { get; set; }
        public string? ProjectName { get; set; }
        public decimal? ProjectValue { get; set; }
        public string?  ProjectContractTawk { get; set; }
        public string? ProjectRecieveLoaction { get; set; }
        public string? ProjectObserveName { get; set; }
        public string? ProjectObserveMobile { get; set; }
        public string? ProjectObserveMail { get; set; }
        public string?  ProjectTaslemFirst { get; set; }
        public int? FDamanID { get; set; }
        public int? LDamanID { get; set; }
        public decimal? NesbaEngaz { get; set; }
        public string? Takeem { get; set; }
        public bool? ProjectContractTawkCh { get; set; }
        public bool? ProjectRecieveLoactionCh { get; set; }
        public bool? ProjectTaslemFirstCh { get; set; }
        public bool? ContractCh { get; set; }
        public int? PeriodProject { get; set; }
        public string? AgentDate { get; set; }
        public string? AgentHijriDate { get; set; }
        public string? StreetName { get; set; }
        public string? MainText { get; set; }
        public string? BranchText { get; set; }
        public int? BranchId { get; set; }
        public string? TaskText { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerName_W { get; set; }

        public string? ProjectSubTypeName { get; set; }
        public string? ProjectTypesName { get; set; }
        public string? ProjectMangerName { get; set; }
        public string? CityName { get; set; }
        public string? TransactionTypeName { get; set; }
        public string? RegionTypeName { get; set; }
        public decimal? ExpectedTime { get; set; }
        public int FileCount { get; set; }
        public string?  CurrentMainPhase { get; set; }
        public string? CurrentSubPhase { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Income { get; set; }
        public int ProjectWorkerCount { get; set; }
        public int TaskRemainingCount { get; set; }
        public decimal? TaskNotStarted { get; set; }
        public decimal? TaskInProgress { get; set; }
        public decimal? TaskDone { get; set; }
        public decimal? TaskLate { get; set; }
        public decimal? PercentComplete { get; set; }
        public int? NoOfDays { get; set; }

        public int? ReasonID { get; set; }
        public string? ReasonText { get; set; }
        public string? FinishReason { get; set; }

        
        public string? DateOfFinish { get; set; }

        public string? FirstProjectDate { get; set; }
        public string? FirstProjectExpireDate { get; set; }

        public string? SettingNoP { get; set; }
        public string? SettingNoteP { get; set; }

        public int TaskExecPercentage_Count { get; set; }
        public int? TaskExecPercentage_Sum { get; set; }
        public decimal? WorkOrder_Sum { get; set; }
        public int? WorkOrder_Count { get; set; }

        public int? ActiveMainPhaseId { get; set; }
        public int? ActiveSubPhaseId { get; set; }

        public int? ContractId { get; set; }

        public decimal? CostE { get; set; }
        public decimal? CostE_Credit { get; set; }
        public decimal? CostE_Depit { get; set; }

        public decimal? CostS { get; set; }


        public decimal? CostE_W { get; set; }
        public decimal? CostE_Credit_W { get; set; }
        public decimal? CostE_Depit_W { get; set; }

        public decimal? CostS_W { get; set; }

        public decimal? Oper_expeValue { get; set; }

        public string? CostE_S_Type { get; set; }

        public int? StopProjectType { get; set; }
        public string? Co_opOfficeName { get; set; }
        public string? Co_opOfficeEmail { get; set; }
        public string? Co_opOfficePhone { get; set; }
        public int? ContractorSelectId { get; set; }

        public string? ContractorEmail_T { get; set; }
        public string? ContractorPhone_T { get; set; }
        public string? ContractorCom_T { get; set; }
        public int? CostCenterId { get; set; }

        public int? MunicipalId { get; set; }
        public int? SubMunicipalityId { get; set; }


        public string? MunicipalName { get; set; }
        public string? SubMunicipalityName { get; set; }

        public string? ProBuildingDisc { get; set; }

        public string? ProPieceNumber { get; set; }

        public string? TimeStr { get; set; }
        public string? AddUser { get; set; }
        public string? UpdateUser { get; set; }
        public string? AddUserJob { get; set; }
        public string? ProjectTaskExist { get; set; }
        public string? ProjectInvoiceExist { get; set; }
        public string? ContractValue { get; set; }
        public bool IsNotSent { get; set; }
        public int? OffersPricesId { get; set; }
        public string? OfferPriceNoName { get; set; }

        public int? DepartmentId { get; set; }
        public int? MotionProject { get; set; }
        public string? MotionProjectDate { get; set; }
        public string? MotionProjectNote { get; set; }

        public int? importantid { get; set; }
        public int? Isimportant { get; set; }

        public int? flag { get; set; }
        public int? TypeCode { get; set; }
        public string? Cons_components { get; set; }

        public string? AddedUserImg { get; set; }
        public string? ProjectManagerImg { get; set; }
        public int? Plustimecount { get; set; }
        public int? SkipCount { get; set; }
        public string? CostCenterName { get; set; }
        public string? CostCenterCode { get; set; }
        public string? StopProjectDate { get; set; }

        public string? DateFrom_Search { get; set; }
        public string? DateTo_Search { get; set; }
        public int? DestinationsUpload { get; set; }
        public bool? select { get; set; }
        public bool? Insert { get; set; }
        public bool? Update { get; set; }
        public bool? Delete { get; set; }
        public int? FilterType { get; set; }


        public bool? IsCertified { get; set; }
        public string? CertifiedCode { get; set; }
        public int? ImplementationDuration { get; set; }
        public int? OfferValidity { get; set; }

        public List<ProjectWorkersVM>? ProjectWorkers { get; set; }
        public List<ProjectPhasesTasksVM>? ProjectPhasesTasks { get; set; }
    }
}
