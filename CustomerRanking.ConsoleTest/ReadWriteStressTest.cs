using CustomerRanking.Application.Iservice;
using CustomerRanking.Application.Service;
using CustomerRanking.Common.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerRanking.ConsoleTest
{
    public class ReadWriteStressTest
    {
        public static async Task Do()
        {
            Console.WriteLine($"Starting to init for {TestConstant.initCustomerCount} customers...");

            var service = new RankingSortListService();
            var sortSetService = new RankingSortSetService();
            var sortDicService = new RankingSortDicService();

            InitData(service, "SortedList");
            InitData(sortSetService, "SortedSet");
            InitData(sortDicService, "SortedDictionary");

            Console.WriteLine("------------------------");
            var service_LockSlim = new RankingSortListService_LockSlim();
            var sortSetService_LockSlim = new RankingSortSetService_LockSlim();
            var sortDicService_LockSlim = new RankingSortDicService_LockSlim();
            InitData(service_LockSlim, "SortedList_LockSlim");
            InitData(sortSetService_LockSlim, "SortedSet_LockSlim");
            InitData(sortDicService_LockSlim, "SortedDictionary_LockSlim");
            Console.WriteLine("------------------------");
            Console.WriteLine($"starting read-write[readRatio={TestConstant.readRatio}] stress test: {TestConstant.taskCount} tasks,each task update {TestConstant.eachTaskUpdateCount} customers...");

            var t1 = RunWriteAndReaderTest(service, "SortedList");
            var t2 = RunWriteAndReaderTest(sortSetService, "SortedSet");
            var t3 = RunWriteAndReaderTest(sortDicService, "SortedDictionary");
            await Task.WhenAll(t1, t2, t3);
            Console.WriteLine("------------------------");

            var t4 = RunWriteAndReaderTest(service_LockSlim, "SortedList_LockSlim");
            var t5 = RunWriteAndReaderTest(sortSetService_LockSlim, "SortedSet_LockSlim");
            var t6 = RunWriteAndReaderTest(sortDicService_LockSlim, "SortedDictionary_LockSlim");
            await Task.WhenAll(t4, t5, t6);

        }
        static void InitData(IRankingService service, string label)
        {
            var sw = Stopwatch.StartNew();
            for (long id = 1; id <= TestConstant.initCustomerCount; id++)
                service.UpdateScore(id, 500m);
            sw.Stop();
            Console.WriteLine($"{label.PadRight(26)} Elapsed time: {sw.ElapsedMilliseconds} ms");
        }
        static async Task RunWriteAndReaderTest(IRankingService service, string label)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            var cts = new CancellationTokenSource();
            for (int i = 0; i < TestConstant.taskCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var rnd = new Random(Guid.NewGuid().GetHashCode());
                    for (int j = 0; j < TestConstant.eachTaskUpdateCount; j++)
                    {
                        if (rnd.NextDouble() < TestConstant.readRatio)
                        {
                            if (rnd.Next(2) == 0)
                                service.GetCustomersByRank(100, 200);
                            else
                                service.GetCustomersById(rnd.Next(1, TestConstant.initCustomerCount + 1), 49, 78);
                        }
                        else
                        {
                            long id = rnd.Next(1, TestConstant.initCustomerCount + 1);
                            decimal score = rnd.Next(TestConstant.minScore, TestConstant.maxScore + 1);
                            service.UpdateScore(id, score);
                        }
                    }
                }, cts.Token));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            Console.WriteLine($"{label.PadRight(26)} Mixed Elapsed time: {sw.ElapsedMilliseconds} ms");
        }
    }
}
