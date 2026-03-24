using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Models.RequestModels
{
    public class SubmissionPathUpdateRequest
    {
        [Required(ErrorMessage = "Path can't be empty")]
        public string ProjectFolder { get; set; } = string.Empty;
    }
}
