using System;
using System.Threading;
using BenchmarkDotNet.Running;
using ProjNET.Tests.Performance;

namespace ProjNet.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PerformanceTests>();

            Console.WriteLine("Press Spacebar");
            while (Console.ReadKey().Key != ConsoleKey.Spacebar)
                Thread.Sleep(500);

        }
    }
}
