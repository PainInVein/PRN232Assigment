namespace PRN232.NMS.API.Models.ResponseModels
{
    public class ResponseDTO<T>
    {
        public string Message { get; set; }

        public bool IsSuccess { get; set; }
        public T? Data { get; set; }

        public string? Errors { get; set; }


        public ResponseDTO(string message, bool isSuccess, T? data, string? errors)
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
