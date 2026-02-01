
using BenchmarkDotNet.Running;
using CustomerRanking.ConsoleTest;

//1,for benchmark testing

//BenchmarkRunner.Run<BenchmarkTest>();

//2,for write only testing
//await WriteOnlyStressTest.Do();

//3 ,for read-write stress testing
//await ReadWriteStressTest.Do();


//4,for benchmark testing with skip list
//BenchmarkRunner.Run<BenchmarkTest_SkipList>();

//5,for write only testing with skip list
//await WriteOnlyStressTest_SkipList.Do();

//6 ,for read-write stress testing with skip list
await ReadWriteStressTest_SkipList.Do();