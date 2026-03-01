using PRN232.NMS.Repo.EntityModels;
using PRN232.NMS.Services.Interfaces;
using Repositories;

namespace PRN232.NMS.Services
{
    public class TagService : ITagService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TagService() => _unitOfWork ??= new UnitOfWork();

        public async Task<Tag> GetByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.TagRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n");
            }
            return null;
        }

        public async Task<(List<Tag> Items, int TotalItems)> GetTagsPagedAsync(int page, int pageSize)
        {
            try
            {
                var items = await _unitOfWork.TagRepository
                    .GetAllSimpleAsync((page - 1) * pageSize, pageSize);

                var totalItems = await _unitOfWork.TagRepository.CountAsync();

                return (items, totalItems);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
