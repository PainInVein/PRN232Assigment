namespace PRN232.NMS.API.Models.ResponseModels
{
    public class ResponseDTO<T>
    {
        public string Message { get; set; }

        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public ResponseDTO() { }
        public ResponseDTO(string message, bool isSuccess, T? data, object? errors)
        {
            Message = message;
            IsSuccess = isSuccess;
            Data = data;
            Errors = errors;
        }

        //public ResponseDTO(string message, bool isSuccess, string errors)
        //{
        //    Message = message;
        //    IsSuccess = isSuccess;
        //    Errors = errors;
        //}

        //public ResponseDTO(string message, bool isSuccess)
        //{
        //    Message = message;
        //    IsSuccess = isSuccess;
        //}
    }
}
