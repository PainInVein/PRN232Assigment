namespace PRN232.NMS.API.Models.ResponseModels.TagResponses
{
    public class GetAllResponse
    {
        public int TagId { get; set; }

        public string TagName { get; set; } = null!;

        public string? Note { get; set; }
    }
}
