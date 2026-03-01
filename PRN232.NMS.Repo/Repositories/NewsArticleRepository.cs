using EVCMS.Repositories.BinhLS.Basic;
using Microsoft.EntityFrameworkCore;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.EntityModels;

namespace PRN232.NMS.Repo.Repositories
{
    public class NewsArticleRepository : GenericRepository<NewsArticle>
    {
        public NewsArticleRepository() { }

        public NewsArticleRepository(Prn312classDbContext context) : base(context) { }

        public async Task<List<NewsArticle>> GetAllAsync()
        {
            var items = await _context.NewsArticles
                .Select(a => new NewsArticle
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    NewsContent = a.NewsContent,
                    NewsSource = a.NewsSource,
                    CategoryId = a.CategoryId,
                    NewsStatusId = a.NewsStatusId,
                    CreatedById = a.CreatedById,
                    UpdatedById = a.UpdatedById,

                    Category = a.Category == null ? null : new Category
                    {
                        CategoryId = a.Category.CategoryId,
                        CategoryName = a.Category.CategoryName,
                        CategoryDescription = a.Category.CategoryDescription
                    },

                    CreatedBy = a.CreatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.CreatedBy.AccountId,
                        AccountName = a.CreatedBy.AccountName,
                        AccountEmail = a.CreatedBy.AccountEmail,
                        AccountRole = a.CreatedBy.AccountRole
                    },

                    UpdatedBy = a.UpdatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.UpdatedBy.AccountId,
                        AccountName = a.UpdatedBy.AccountName,
                        AccountEmail = a.UpdatedBy.AccountEmail,
                        AccountRole = a.UpdatedBy.AccountRole
                    },

                    Tags = a.Tags.Select(t => new Tag
                    {
                        TagId = t.TagId,
                        TagName = t.TagName,
                        Note = t.Note
                    }).ToList()
                })
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return items ?? new List<NewsArticle>();
        }

        public async Task<NewsArticle> GetByIdAsync(int id)
        {
            var article = await _context.NewsArticles
                .Where(a => a.NewsArticleId == id)
                .Select(a => new NewsArticle
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    NewsContent = a.NewsContent,
                    NewsSource = a.NewsSource,
                    CategoryId = a.CategoryId,
                    NewsStatusId = a.NewsStatusId,
                    CreatedById = a.CreatedById,
                    UpdatedById = a.UpdatedById,

                    Category = a.Category == null ? null : new Category
                    {
                        CategoryId = a.Category.CategoryId,
                        CategoryName = a.Category.CategoryName,
                        CategoryDescription = a.Category.CategoryDescription
                    },

                    CreatedBy = a.CreatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.CreatedBy.AccountId,
                        AccountName = a.CreatedBy.AccountName,
                        AccountEmail = a.CreatedBy.AccountEmail,
                        AccountRole = a.CreatedBy.AccountRole
                    },

                    UpdatedBy = a.UpdatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.UpdatedBy.AccountId,
                        AccountName = a.UpdatedBy.AccountName,
                        AccountEmail = a.UpdatedBy.AccountEmail,
                        AccountRole = a.UpdatedBy.AccountRole
                    },

                    Tags = a.Tags.Select(t => new Tag
                    {
                        TagId = t.TagId,
                        TagName = t.TagName,
                        Note = t.Note
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return article ?? new NewsArticle();
        }

        public async Task<NewsArticle?> GetByIdWithRelationsAsync(int id)
        {
            return await _context.NewsArticles
                .Where(a => a.NewsArticleId == id)
                .Select(a => new NewsArticle
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    NewsContent = a.NewsContent,
                    NewsSource = a.NewsSource,
                    CategoryId = a.CategoryId,
                    NewsStatusId = a.NewsStatusId,
                    CreatedById = a.CreatedById,
                    UpdatedById = a.UpdatedById,

                    Category = a.Category == null ? null : new Category
                    {
                        CategoryId = a.Category.CategoryId,
                        CategoryName = a.Category.CategoryName,
                        CategoryDescription = a.Category.CategoryDescription
                    },

                    CreatedBy = a.CreatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.CreatedBy.AccountId,
                        AccountName = a.CreatedBy.AccountName,
                        AccountEmail = a.CreatedBy.AccountEmail,
                        AccountRole = a.CreatedBy.AccountRole
                    },

                    UpdatedBy = a.UpdatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.UpdatedBy.AccountId,
                        AccountName = a.UpdatedBy.AccountName,
                        AccountEmail = a.UpdatedBy.AccountEmail,
                        AccountRole = a.UpdatedBy.AccountRole
                    },

                    Tags = a.Tags.Select(t => new Tag
                    {
                        TagId = t.TagId,
                        TagName = t.TagName,
                        Note = t.Note
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<NewsArticle>> SearchAsync(
            string? title = null,
            int? categoryId = null,
            int? statusId = null,
            int? createdById = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.NewsArticles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(a => a.NewsTitle.Contains(title));

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId.Value);

            if (statusId.HasValue)
                query = query.Where(a => a.NewsStatusId == statusId.Value);

            if (createdById.HasValue)
                query = query.Where(a => a.CreatedById == createdById.Value);

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedDate <= toDate.Value);

            var items = await query
                .Select(a => new NewsArticle
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    NewsContent = a.NewsContent,
                    NewsSource = a.NewsSource,
                    CategoryId = a.CategoryId,
                    NewsStatusId = a.NewsStatusId,
                    CreatedById = a.CreatedById,
                    UpdatedById = a.UpdatedById,

                    Category = a.Category == null ? null : new Category
                    {
                        CategoryId = a.Category.CategoryId,
                        CategoryName = a.Category.CategoryName,
                        CategoryDescription = a.Category.CategoryDescription
                    },

                    CreatedBy = a.CreatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.CreatedBy.AccountId,
                        AccountName = a.CreatedBy.AccountName,
                        AccountEmail = a.CreatedBy.AccountEmail,
                        AccountRole = a.CreatedBy.AccountRole
                    },

                    UpdatedBy = a.UpdatedBy == null ? null : new SystemAccount
                    {
                        AccountId = a.UpdatedBy.AccountId,
                        AccountName = a.UpdatedBy.AccountName,
                        AccountEmail = a.UpdatedBy.AccountEmail,
                        AccountRole = a.UpdatedBy.AccountRole
                    },

                    Tags = a.Tags.Select(t => new Tag
                    {
                        TagId = t.TagId,
                        TagName = t.TagName,
                        Note = t.Note
                    }).ToList()
                })
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return items ?? new List<NewsArticle>();
        }

        // Lightweight version – suitable for lists/grids (no content, no tags, minimal relations)
        public async Task<List<NewsArticle>> GetAllHeadersAsync()
        {
            var items = await _context.NewsArticles
                .Select(a => new NewsArticle
                {
                    NewsArticleId = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    Headline = a.Headline,
                    CreatedDate = a.CreatedDate,
                    NewsSource = a.NewsSource,
                    CategoryId = a.CategoryId,
                    NewsStatusId = a.NewsStatusId,
                    CreatedById = a.CreatedById,
                    UpdatedById = a.UpdatedById,

                    // No NewsContent, no Tags, no UpdatedBy (to keep it light)
                })
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return items ?? new List<NewsArticle>();
        }
    }
}