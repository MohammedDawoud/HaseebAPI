﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace TaamerProject.Models
{
    public class UserIDInfo
    {
        public int MachineNumber { get; set; }
        public int EnrollNumber { get; set; }
        public int BackUpNumber { get; set; }
        public int Privelage { get; set; }
        public int Enabled { get; set; }

    }
}
