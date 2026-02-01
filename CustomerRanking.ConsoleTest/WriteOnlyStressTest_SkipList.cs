using CustomerRanking.Application.Iservice;
using CustomerRanking.Application.Service;
using CustomerRanking.Common.Constants;
using System.Diagnostics;

namespace CustomerRanking.ConsoleTest
{
    public class WriteOnlyStressTest_SkipList
    {
        public static async Task Do()
        {
            Console.WriteLine($"Starting to init for {TestConstant.initCustomerCount} customers...");

            var service = new RankingService();
            var service_SortSet = new RankingSortSetService();

            InitData(service, "SkipList");
            InitData(service_SortSet, "SortedSet");
            Console.WriteLine("------------------------");
            Console.WriteLine($"starting write only stress test: {TestConstant.taskCount} tasks,each task update {TestConstant.eachTaskUpdateCount} customers...");

            var t1 = RunWriteTest(service, "SkipList");
            var t2 = RunWriteTest(service_SortSet, "SortedSet");
            await Task.WhenAll(t1, t2);

        }
        static void InitData(IRankingService service, string label)
        {
            var sw = Stopwatch.StartNew();
            for (long id = 1; id <= TestConstant.initCustomerCount; id++)
                service.UpdateScore(id, 500m);
            sw.Stop();
            Console.WriteLine($"{label.PadRight(26)} Elapsed time: {sw.ElapsedMilliseconds} ms");
        }
        static async Task RunWriteTest(IRankingService service, string label)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (int i = 0; i < TestConstant.taskCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var localRandom = new Random(Guid.NewGuid().GetHashCode());
                    for (int j = 0; j < TestConstant.eachTaskUpdateCount; j++)
                    {
                        long id = (long)localRandom.Next(1, TestConstant.initCustomerCount + 1);
                        decimal score = localRandom.Next(TestConstant.minScore, TestConstant.maxScore + 1);
                        service.UpdateScore(id, score);
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            Console.WriteLine($"{label.PadRight(26)} Elapsed time: {sw.ElapsedMilliseconds} ms");
        }

    }
}
