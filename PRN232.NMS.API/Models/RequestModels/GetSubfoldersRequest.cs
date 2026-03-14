using System.ComponentModel.DataAnnotations;

namespace PRN232.NMS.API.Models.RequestModels
{
    public class GetSubfoldersRequest
    {
        [Required(ErrorMessage = "ProjectFolder is required.")]
        [StringLength(260, MinimumLength = 1, ErrorMessage = "ProjectFolder must be between 1 and 260 characters.")]
        public string ProjectFolder { get; set; }
    }
}
