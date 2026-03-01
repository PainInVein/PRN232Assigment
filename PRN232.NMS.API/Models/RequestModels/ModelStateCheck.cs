using Microsoft.AspNetCore.Mvc.ModelBinding;
using PRN232.NMS.API.Models.ResponseModels;

namespace PRN232.NMS.API.Models.RequestModels
{
    public interface IModelStateCheck
    {
        string CheckModelState<T>(ModelStateDictionary modelState);
    }

    public class ModelStateCheck : IModelStateCheck
    {
        public string CheckModelState<T>(ModelStateDictionary modelState)
        {
            string result;

            if (!modelState.IsValid)
            {
                var errors = modelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var errorMessage = string.Join("; ", errors);

                result = errorMessage;

                return result;
            }

            return result = string.Empty;
        }
    }
}
