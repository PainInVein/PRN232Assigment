namespace PRN232.NMS.API.Models.ResponseModels.TagResponses
{
    public class GetByIdResponse
    {
        public int TagId { get; set; }

        public string TagName { get; set; } = null!;

        public string? Note { get; set; }

        public List<RelatedNewsArticleResponse> NewsArticles { get; set; } = new();
    }

    public class RelatedNewsArticleResponse
    {
        public int NewsArticleId { get; set; }

        public string NewsTitle { get; set; } = null!;

        public string? Headline { get; set; }

        public DateTime CreatedDate { get; set; }

        public string NewsContent { get; set; } = null!;

        public string? NewsSource { get; set; }

        public int CategoryId { get; set; }

        public int NewsStatusId { get; set; }

        public int CreatedById { get; set; }

        public int? UpdatedById { get; set; }
    }
}
