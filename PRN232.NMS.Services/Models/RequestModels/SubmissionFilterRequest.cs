using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Models.RequestModels
{
    public class SubmissionFilterRequest : PagedRequest
    {
        public string? SearchName { get; set; }
        public string? SortOption { get; set; }
        public List<string>? StatusList { get; set; }
    }
}
