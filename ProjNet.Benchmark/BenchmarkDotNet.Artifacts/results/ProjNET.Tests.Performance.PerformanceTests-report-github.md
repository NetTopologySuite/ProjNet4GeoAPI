``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.590 (1803/April2018Update/Redstone4)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.104
  [Host]     : .NET Core 2.2.2 (CoreCLR 4.6.27317.07, CoreFX 4.6.27318.02), 64bit RyuJIT
  Job-HRIMCP : .NET Core 2.2.2 (CoreCLR 4.6.27317.07, CoreFX 4.6.27318.02), 64bit RyuJIT

Server=True  

```
|                          Method |    Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------------- |--------:|---------:|---------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|     TestCoordinateArraySequence | 2.533 s | 0.0326 s | 0.0289 s |  1.00 |    0.00 |   5000.0000 |   2000.0000 |   1000.0000 |           1172.7 MB |
|  TestCoordinateArraySequenceOpt | 2.487 s | 0.0497 s | 0.0830 s |  0.98 |    0.05 |   6000.0000 |   5000.0000 |   4000.0000 |          1620.64 MB |
|     TestPackedDoubleSequenceOpt | 1.797 s | 0.0342 s | 0.0351 s |  0.71 |    0.01 |  10000.0000 |   9000.0000 |   6000.0000 |           756.81 MB |
| TestDotSpatialAffineSequenceOpt | 1.778 s | 0.0159 s | 0.0133 s |  0.70 |    0.01 |   8000.0000 |   7000.0000 |   3000.0000 |           781.33 MB |
|        TestPackedDoubleSequence | 2.122 s | 0.0309 s | 0.0289 s |  0.84 |    0.02 |  11000.0000 |  10000.0000 |   7000.0000 |           756.81 MB |
|    TestDotSpatialAffineSequence | 2.105 s | 0.0130 s | 0.0115 s |  0.83 |    0.01 |   7000.0000 |   6000.0000 |   3000.0000 |           781.33 MB |
