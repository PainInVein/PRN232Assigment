using AutoMapper;
using PRN232.NMS.Repo.Entities;
using PRN232.NMS.Services.BusinessModel;
using PRN232.NMS.Services.Helpers.HelperEntities;
using PRN232.NMS.Services.Models.RequestModels;
using PRN232.NMS.Services.Models.ResponseModels;

namespace PRN232.NMS.Services.Models.MappingTool
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<GradingRequest, SingleStudentGrading>();
            CreateMap<GradingResultWithListLogs, GradingResultSingleResponse>();
            CreateMap<GradingAllResult, GradingResultAllResponse>();
            CreateMap<GradingResult, SubmissionsGetAllResponse>();
            CreateMap<GradingResult, GetSubmissionByIdResponse>();
        }
    }
}
