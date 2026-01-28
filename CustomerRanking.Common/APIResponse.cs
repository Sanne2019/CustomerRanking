

namespace CustomerRanking.Common
{
    public class APIResponse
    {
        public bool IsSuccess { get; }
        public object Data { get; }
        public string ErrorMessage { get; }

        private APIResponse(bool success, object data, string error)
        {
            IsSuccess = success;
            Data = data;
            ErrorMessage = error;
        }

        public static APIResponse Success(object data) => new(true, data, null);
        public static APIResponse Failure(string error) => new(false, null, error);
    }
}
