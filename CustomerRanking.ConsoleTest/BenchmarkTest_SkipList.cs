using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CustomerRanking.Application.Service;
using CustomerRanking.Common.Constants;
using System;

namespace CustomerRanking.ConsoleTest
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class BenchmarkTest_SkipList
    {
        private readonly Random _random = new();
        private RankingService _service;

        [Params(100_000, 1_000_000)]
        public int CustomerCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _service = new RankingService();

            for (int i = 1; i <= CustomerCount; i++)
            {
                decimal score = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
                _service.UpdateScore(i, score);
            }
        }

        [Benchmark]
        public void GetCustomersByRank()
        {
            int startRank = _random.Next(1, Math.Max(1, CustomerCount - 101));
            int endRank = startRank + 100;

            _service.GetCustomersByRank(startRank, endRank);
        }

        [Benchmark]
        public void GetCustomersById()
        {
            long targetId = _random.Next(1, CustomerCount + 1);
            _service.GetCustomersById(targetId, 10, 10);
        }

        [Benchmark]
        public void UpdateScore()
        {
            for (int i = 0; i < 100; i++)
            {
                long id = _random.Next(1, CustomerCount + 1);
                decimal newScore = _random.Next(TestConstant.minScore, TestConstant.maxScore + 1);
                _service.UpdateScore(id, newScore);
            }
        }
    }
}