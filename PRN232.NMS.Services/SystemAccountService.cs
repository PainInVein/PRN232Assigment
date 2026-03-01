using PRN232.NMS.Repo.EntityModels;
using PRN232.NMS.Services.Interfaces;
using Repositories;

namespace PRN232.NMS.Services
{
    public class SystemAccountService : ISystemAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SystemAccountService() => _unitOfWork ??= new UnitOfWork();

        public async Task<SystemAccount> GetUserAccount(string username, string password)
        {
            try
            {
                return await _unitOfWork.SystemUserAccountRepository.GetUsernameAsync(username, password);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n");
            }
            return null;

        }

        public async Task<SystemAccount> GetAllUser()
        {
            try
            {
                return await _unitOfWork.SystemUserAccountRepository.GetAllUserAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n");
            }
            return null;
        }
    }
}
