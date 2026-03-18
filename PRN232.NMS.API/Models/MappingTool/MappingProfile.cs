using AutoMapper;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Repo.Entities;

namespace PRN232.NMS.API.Models.MappingTool
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
                CreateMap<GradingResult, GradingResultDTO>().ReverseMap();
        }
    }
}
