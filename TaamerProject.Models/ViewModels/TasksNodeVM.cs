using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace TaamerProject.Models
{
    public class TasksNodeVM
    {
        public IEnumerable<SettingsVM>? nodeDataArray { get; set; }
        public IEnumerable<DependencySettingsVM>? linkDataArray { get; set; }
        public IEnumerable<SettingsVM>? firstLevelNode { get; set; }
    }
}
