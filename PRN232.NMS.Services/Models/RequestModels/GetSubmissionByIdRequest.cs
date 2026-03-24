using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Models.RequestModels
{
    public class GetSubmissionByIdRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Article ID must be greater than 0")]
        public int studentId { get; set; }
    }
}
