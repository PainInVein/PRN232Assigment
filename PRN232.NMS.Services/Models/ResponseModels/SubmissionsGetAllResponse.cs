using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Models.ResponseModels
{
    public class SubmissionsGetAllResponse
    {
        public int StudentId { get; set; }

        public string? StudentName { get; set; }

        public string? ProjectFolder { get; set; }

        public int? Score { get; set; }

        public decimal? Points { get; set; }

        public string? Status { get; set; }
    }
}
