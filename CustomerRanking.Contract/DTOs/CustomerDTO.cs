using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerRanking.Contract.DTOs
{
    public class CustomerDTO
    {
        public long CustomerId { get; set; }
        public decimal Score { get; set; }
    }
    public class CustomerRankDTO
    {
        public decimal Score { get; set; }
        public long CustomerId { get; set; }
        public int Rank { get; set; }
    }
}
