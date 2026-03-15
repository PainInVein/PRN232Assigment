using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Repositories;

namespace Repositories
{
    public interface IUnitOfWork
    {
        GradingResultRepository GradingResultRepository { get; }
        
        Task<int> SaveChangesAsync();
    }
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Prn232lab3Context _context;
        private GradingResultRepository _gradingResultRepository;

        public UnitOfWork() => _context ??= new Prn232lab3Context();

        public GradingResultRepository GradingResultRepository
        {
            get
            {
                return _gradingResultRepository ??= new GradingResultRepository(_context);
            }
        }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
