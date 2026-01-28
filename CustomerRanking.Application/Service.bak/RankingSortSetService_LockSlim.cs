using CustomerRanking.Application.Iservice;
using CustomerRanking.Common;
using CustomerRanking.Common.Common;
using CustomerRanking.Common.Constants;
using CustomerRanking.Common.Extensions;
using CustomerRanking.Contract.DTOs;

namespace CustomerRanking.Application.Service
{
    /// <summary>
    /// SortedSet + ReaderWriterLockSlim
    /// </summary>
    public class RankingSortSetService_LockSlim : IRankingService
    {
        private readonly Dictionary<long, CustomerDTO> _customers = new();
        private readonly Dictionary<long, decimal> _customoer_Id_Score_Map = new();
        private readonly SortedSet<(decimal score, long id)> _leaderBoard = new();
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
                {//all customers whose score is greater than zero participate in a competition
                    _customoer_Id_Score_Map[customerId] = customer.Score;

                    var newKey = CommonFunction.GetKey(customer.Score, customer.CustomerId);
                    _leaderBoard.Add(newKey);
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
            using (new ReaderLock(_rwLock))
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
            using (new ReaderLock(_rwLock))
            {
                if (!_customers.TryGetValue(customerId, out var customer))
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotFound);
                }

                if (!_customoer_Id_Score_Map.ContainsKey(customerId))
                {
                    return APIResponse.Failure(ApplicationConstant.customerNotRanked);
                }

                int index = -1;
                int i = 0;
                foreach (var item in _leaderBoard)
                {
                    if (item.id == customerId)
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
                var id = entries[i].id;
                if (_customers.TryGetValue(id, out var c))
                {
                    list.Add(new CustomerRankDTO { CustomerId = c.CustomerId, Score = c.Score, Rank = startIndex + 1 + i });
                }
            }
            return list;
        }
    }
}