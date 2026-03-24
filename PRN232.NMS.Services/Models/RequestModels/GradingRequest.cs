using System.ComponentModel.DataAnnotations;

namespace PRN232.NMS.Services.Models.RequestModels
{
    public class GradingRequest
    {
        [Required(ErrorMessage = "Project path is required")]
        public string ProjectFolder { get; set; } = null!;  // C:\\Users\\Admin\\Desktop\\SE171286_ThinhVQSE171286
    }
}
