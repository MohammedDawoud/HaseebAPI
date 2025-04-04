﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace TaamerProject.Models
{
    public class ProSettingDetails : Auditable
    {
        public int ProSettingId { get; set; }
        public string? ProSettingNo { get; set; }
        public string? ProSettingNote { get; set; }
        public int? ProjectTypeId { get; set; }
        public int? ProjectSubtypeId { get; set; }
        public int? AddUser { get; set; }
        public  Users? Users { get; set; }
        public ProjectType? ProjectType { get; set; }
        public ProjectSubTypes? ProjectSubTypes { get; set; }

    }
}
