using Azure;
using Microsoft.EntityFrameworkCore;
using PRN232.NMS.Repo.Basic;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PRN232.NMS.Repo.Repositories
{
    public class GradingResultRepository : GenericRepository<GradingResult>
    {
        public GradingResultRepository() { }

        public GradingResultRepository(Prn232lab3Context context) : base(context) { }

        public async Task<(List<GradingResult> Items, int TotalItems)> GetAllSubmissionsAsync(int skip, int take, string? searchTerm, string? sortOption, List<string>? status)
        {
            var query = _context.GradingResults.AsQueryable();


            //filter by status
            if (status != null && status.Count > 0)
            {
                var normalizedStatus = status.Select(s => s.ToLower()).ToList();

                query = query.Where(t =>
                    normalizedStatus.Contains(t.Status!.ToLower())
                );
            }


            //search by student name
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.StudentName!.Contains(searchTerm));
            }

            //sort by score
            query = sortOption?.ToLower() switch
            {
                "desc" => query.OrderByDescending(x => x.Score),
                "asc" => query.OrderBy(x => x.Score),
                _ => query.OrderBy(x => x.StudentId)
            };


            var items = await query.Select(gr => new GradingResult
            {
                StudentId = gr.StudentId,
                StudentName = gr.StudentName,
                ProjectFolder = gr.ProjectFolder,
                Score = gr.Score,
                Points = gr.Points,
                Status = gr.Status
            })
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var totalItems = await query.CountAsync();

            return (items ?? new List<GradingResult>(), totalItems);
        }

        public async Task<GradingResult?> GetByIdDetailedAsync(int id)
        {
            return await _context.GradingResults
                .Where(n => n.StudentId == id)
                .Select(n => new GradingResult
                {
                    StudentId = n.StudentId,
                    StudentName = n.StudentName,
                    ProjectFolder = n.ProjectFolder,
                    Score = n.Score,
                    Logs = n.Logs,
                    Points = n.Points,
                    Status = n.Status
                })
                .FirstOrDefaultAsync();
        }
    }
}
