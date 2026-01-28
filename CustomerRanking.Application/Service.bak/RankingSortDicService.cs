using CustomerRanking.Application.Iservice;
using CustomerRanking.Common;
using CustomerRanking.Common.Common;
using CustomerRanking.Common.Constants;
using CustomerRanking.Contract.DTOs;

namespace CustomerRanking.Application.Service
{
    /// <summary>
    /// SortedDictionary + Lock
    /// </summary>
    public class RankingSortDicService : IRankingService
    {
        private readonly Dictionary<long, CustomerDTO> _customers = new();
        private readonly Dictionary<long, decimal> _customoer_Id_Score_Map = new();
        private readonly SortedDictionary<(decimal score, long id), CustomerDTO> _leaderBoard
            = new();
        private readonly object _lock = new();

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
            lock (_lock)
            {
                if (!_customers.TryGetValue(customerId, out var customer))
                {
                    customer = new CustomerDTO();
                    customer.CustomerId = customerId;
                    customer.Score = addScoreVal;
                    _customers.Add(customerId, customer);
                }
                else
                {
                    customer.Score += addScoreVal;
                }
                if (_customoer_Id_Score_Map.TryGetValue(customerId, out var oldScore))
                {
                    var oldKey = CommonFunction.GetKey(oldScore, customerId);
                    _leaderBoard.Remove(oldKey);
                }

                if (customer.Score > 0)
                {
                    _customoer_Id_Score_Map[customerId] = customer.Score;
                    var newKey = CommonFunction.GetKey(customer.Score, customer.CustomerId);
                    _leaderBoard[newKey] = customer;
                }
                else
                {
                    _customoer_Id_Score_Map.Remove(customerId);
                }
                return APIResponse.Success(customer.Score);
            }
        }

        public APIResponse GetCustomersByRank(int start, int end)
        {
            if (start > end)
            {
                return APIResponse.Failure(ApplicationConstant.startLessEnd);
            }
            start = start < 1 ? 0 : start - 1;
            end = end < 1 ? 0 : end - 1;
            lock (_lock)
            {
                var list = HandleData(start, end);
                return APIResponse.Success(list);
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
            lock (_lock)
            {
                if (!_customers.TryGetValue(customerId, out var customer))
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotFound);
                }
                int index = -1;
                int i = 0;
                foreach (var kv in _leaderBoard)
                {
                    if (kv.Key.id == customerId)
                    {
                        index = i;
                        break;
                    }
                    i++;
                }

                if (index == -1)
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotRanked);
                }
                var list = HandleData((index - high), (index + low));
                return APIResponse.Success(list);
            }
        }
        private List<CustomerRankDTO> HandleData(int start, int end)
        {
            var startIndex = Math.Max(0, start);
            var endIndex = Math.Min(_leaderBoard.Count - 1, end);
            var entries = _leaderBoard.Skip(startIndex).Take(endIndex - startIndex + 1).ToArray();
            var list = new List<CustomerRankDTO>();
            for (int i = 0; i < entries.Length; i++)
            {
                list.Add(new CustomerRankDTO { CustomerId = entries[i].Value.CustomerId, Score = entries[i].Value.Score, Rank = startIndex + 1 + i });
            }
            return list;
        }
    }
}