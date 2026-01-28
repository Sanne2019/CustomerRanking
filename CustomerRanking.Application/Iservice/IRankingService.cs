using CustomerRanking.Common;
using CustomerRanking.Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerRanking.Application.Iservice
{
    public interface IRankingService
    {
        APIResponse UpdateScore(long customerId, decimal addScoreVal);

        APIResponse GetCustomersByRank(int start, int end);
        APIResponse GetCustomersById(long customerId, int high, int low);
    }
}
