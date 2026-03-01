using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Repo.Repositories;

namespace Repositories
{
    public interface IUnitOfWork
    {
        SystemAccountRepository SystemUserAccountRepository { get; }
        TagRepository TagRepository { get; }

        int SaveChangeWithTransaction();
        Task<int> SaveChangeWithTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly Prn312classDbContext _context;
        private SystemAccountRepository _systemAccountRepository;
        private TagRepository _tagRepository;
        public UnitOfWork() => _context ??= new Prn312classDbContext();

        public SystemAccountRepository SystemUserAccountRepository
        {
            get
            {
                return _systemAccountRepository ?? new SystemAccountRepository(_context);
            }
        }

        public TagRepository TagRepository
        {
            get
            {
                return _tagRepository ?? new TagRepository(_context);
            }
        }



        public int SaveChangeWithTransaction()
        {
            int result = 0;

            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    result = _context.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    result = 0;
                    dbContextTransaction.Rollback();
                }
            }
            return result;
        }

        public async Task<int> SaveChangeWithTransactionAsync()
        {
            int result = 0;

            using (var dbContextTransaction = _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.SaveChangesAsync();
                    dbContextTransaction.Result.CommitAsync();
                }
                catch (Exception)
                {
                    result = 0;
                    dbContextTransaction.Result.RollbackAsync();
                }
            }

            return result;
        }
    }
}
