

namespace CustomerRanking.Common.Constants
{
    public class ApplicationConstant
    {
        public const string validCustomerId = "customerId must be greater than zero.";
        public const string scoreRange = "score must be between -1000 and 1000.";
        public const string startLessEnd = "The start rank must be less than the end rank.";
        public const string customerNotRanked = $"This customer is not currently ranked because it doesn't meet the eligibility requirements.";
        public const string customerNotFound = $"This customer was not found.";
    }
    public class TestConstant
    {
        public const int initMinBenchCustomerCount = 100_000;
        public const int initMaxBenchCustomerCount = 1_000_000;
        public const int initCustomerCount = 5_000_000;
        public const int maxScore = 1000;
        public const int minScore = -1000;
        public const int taskCount = 100;
        public const int eachTaskUpdateCount = 100;
        public const double readRatio = 0.7; //x0% for read

    }
}
