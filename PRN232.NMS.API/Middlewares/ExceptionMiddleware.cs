using PRN232.NMS.API.Models.ResponseModels;
using System.Text.Json;

namespace PRN232.NMS.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex}");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, errorMessage) = exception switch
            {
                KeyNotFoundException => (
                    StatusCodes.Status404NotFound,
                    exception.Message,
                    "Resource not found"
                ),

                ArgumentNullException => (
                    StatusCodes.Status400BadRequest,
                    exception.Message,
                    "Required field is missing"
                ),

                ArgumentException => (
                    StatusCodes.Status400BadRequest,
                    exception.Message,
                    "Invalid argument provided"
                ),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    exception.Message
                )
            };

            context.Response.StatusCode = statusCode;

            var response = new ResponseDTO<object>(
                message: message,
                isSuccess: false,
                data: null,
                errors: errorMessage
            );

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
