using System;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ProjNET.Tests.Performance;

namespace ProjNet.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {

#if false
            BenchmarkRunner.Run<PerformanceTests>();
#else
            BenchmarkRunner.Run<PerformanceTests>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .With(Job.Default
                        .WithGcServer(true))
                    .With(MemoryDiagnoser.Default));
#endif
            Console.WriteLine("Press Spacebar");
            while (Console.ReadKey().Key != ConsoleKey.Spacebar)
                Thread.Sleep(500);

        }
    }
}
