﻿using System;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ProjNET.Benchmark.Performance;

namespace ProjNet.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            // make sure that the benchmark is correct
            new PerformanceTests(true);

            BenchmarkRunner.Run<PerformanceTests>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .With(Job.Default
                        .WithGcServer(true))
                    .With(MemoryDiagnoser.Default));

            //Console.WriteLine("Press Spacebar");
            //while (Console.ReadKey().Key != ConsoleKey.Spacebar)
            //    Thread.Sleep(500);

        }
    }
}