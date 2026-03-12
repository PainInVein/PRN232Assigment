using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperEntities
{
    public class GradingResultWithListLogs
    {
        public string ProjectFolder { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Score { get; set; }
        public List<string> Logs { get; set; } = new();
        public DateTime FinishedAt { get; set; }
    }
}
