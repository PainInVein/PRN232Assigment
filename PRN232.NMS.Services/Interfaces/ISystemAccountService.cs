using PRN232.NMS.Repo.EntityModels;

namespace PRN232.NMS.Services.Interfaces
{
    public interface ISystemAccountService
    {
        Task<SystemAccount> GetUserAccount(string username, string password);

        Task<SystemAccount> GetAllUser();
    }
}
