using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Models.ResponseModels
{
    public class GradingResultAllResponse
    {
        public string ProjectFolder { get; set; } = null!;
        public string Status { get; set; } = null!;
        public List<string> Logs { get; set; } = new();
        public DateTime FinishedAt { get; set; }
    }
}
