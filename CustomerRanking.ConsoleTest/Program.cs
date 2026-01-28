
using BenchmarkDotNet.Running;
using CustomerRanking.ConsoleTest;

//1,for benchmark testing

//BenchmarkRunner.Run<BenchmarkTest>();

//2,for write only testing
await WriteOnlyStressTest.Do();

//3 ,for read-write stress testing
//await ReadWriteStressTest.Do();