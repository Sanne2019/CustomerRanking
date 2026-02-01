using CustomerRanking.Application.Iservice;
using CustomerRanking.Common;
using CustomerRanking.Common.Constants;
using CustomerRanking.Common.Extensions;
using CustomerRanking.Contract.DTOs;

namespace CustomerRanking.Application.Service
{
    /// <summary>
    /// Skip List 
    /// </summary>
    public class RankingService : IRankingService
    {
        private readonly Dictionary<long, decimal> _customers = new();
        private readonly Dictionary<long, decimal> _customerScores_Ranked = new();
        private readonly RankedSkipListCustomer _skipListCustomer = new();
        private readonly ReaderWriterLockSlim _rwLock = new();
        public APIResponse UpdateScore(long customerId, decimal addScoreVal)
        {
            if (customerId <= 0)
            {
                return APIResponse.Failure(ApplicationConstant.validCustomerId);
            }
            if (addScoreVal > 1000 || addScoreVal < -1000)
            {
                return APIResponse.Failure(ApplicationConstant.scoreRange);
            }
            using (new WriterLock(_rwLock))
            {

                if (!_customers.TryGetValue(customerId, out decimal currentScore))
                {
                    _customers.Add(customerId, addScoreVal);
                }
                else
                {
                    _customers[customerId] = currentScore + addScoreVal;
                }
                if (_customerScores_Ranked.TryGetValue(customerId, out var oldScore))
                {// Only customers who are ranked need ranking updates, including delete operations
                    _skipListCustomer.Delete(customerId, currentScore);
                }
                var newScore = _customers[customerId];
                if (newScore > 0)
                {//all customers whose score is greater than zero participate in a competition
                    _customerScores_Ranked[customerId] = newScore;
                    _skipListCustomer.Insert(customerId, newScore);
                }
                else
                {// Remove from ranked dictionary if score is <= 0
                    _customerScores_Ranked.Remove(customerId);
                }
                return APIResponse.Success(newScore);
            }
        }
        public APIResponse GetCustomersByRank(int start, int end)
        {
            using (new ReaderLock(_rwLock))
            {
                var result = GetCustomersByRankNoLock(start, end);
                return APIResponse.Success(result);

            }
        }

        public APIResponse GetCustomersById(long customerId, int high, int low)
        {
            if (customerId <= 0)
            {
                return APIResponse.Failure(ApplicationConstant.validCustomerId);
            }
            high = high < 0 ? 0 : high;
            low = low < 0 ? 0 : low;

            using (new ReaderLock(_rwLock))
            {
                if (!_customers.TryGetValue(customerId, out decimal customerScore))
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotFound);
                }

                if (!_customerScores_Ranked.ContainsKey(customerId))
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotRanked);
                }

                int rank = _skipListCustomer.GetRankById(customerId, customerScore);
                if (rank == 0)
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotRanked);
                }
                int start = Math.Max(1, rank - high);
                int end = rank + low;
                var result = GetCustomersByRankNoLock(start, end);
                return APIResponse.Success(result);
            }
        }


        private List<CustomerRankDTO> GetCustomersByRankNoLock(int start, int end)
        {
            var result = new List<CustomerRankDTO>();
            #region Step 1: Find the node at start position as start node
            var startNode = _skipListCustomer.GetNodeByRank(start);
            #endregion

            #region Step 2: Traverse from start node, increment start rank each time, and reassign start node's next node to start node
            // This continues until reaching end position, collecting all elements in between
            int startRank = start;
            while (startNode != null && startRank <= end)
            {
                result.Add(new CustomerRankDTO
                {
                    CustomerId = startNode.CustomerId,
                    Score = startNode.Score,
                    Rank = startRank
                });

                startNode = startNode.NextArray[0];
                startRank++;
            }
            #endregion
            return result;
        }
    }
}