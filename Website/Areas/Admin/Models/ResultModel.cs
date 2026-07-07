namespace Website.Areas.Admin.Models
{
    public class ResultModel<T>
    {
        public bool Succeeded { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }

        public static ResultModel<T> Success(T data)
            => new()
            {
                Succeeded = true,
                Data = data
            };

        public static ResultModel<T> Failure(string message)
            => new()
            {
                Succeeded = false,
                Message = message
            };
    }
}
