
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CustomerRanking.Application.Service;
using CustomerRanking.Common.Constants;

namespace CustomerRanking.ConsoleTest
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class BenchmarkTest
    {
        private readonly Random _random = new();

        [Params(TestConstant.initMinBenchCustomerCount, TestConstant.initMaxBenchCustomerCount)]
        public int CustomerCount { get; set; }

        private RankingSortListService _sortedList;
        private RankingSortSetService _sortedSet;
        private RankingSortDicService _sortedDict;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _sortedList = new RankingSortListService();
            _sortedSet = new RankingSortSetService();
            _sortedDict = new RankingSortDicService();

            for (long i = 1; i <= CustomerCount; i++)
            {
                decimal score = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
                _sortedList.UpdateScore(i, score);
                _sortedSet.UpdateScore(i, score);
                _sortedDict.UpdateScore(i, score);
            }
        }

        [Benchmark]
        public void Update_SortedList()
        {
            long id = (long)_random.Next(1, CustomerCount + 1);
            decimal score = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
            _sortedList.UpdateScore(id, score);
        }

        [Benchmark]
        public void Update_SortedSet()
        {
            long id = (long)_random.Next(1, CustomerCount + 1);
            decimal score = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
            _sortedSet.UpdateScore(id, score);
        }

        [Benchmark]
        public void Update_SortedDictionary()
        {
            long id = (long)_random.Next(1, CustomerCount + 1);
            decimal score = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
            _sortedDict.UpdateScore(id, score);
        }
    }

}