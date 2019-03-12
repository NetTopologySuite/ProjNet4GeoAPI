``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.590 (1803/April2018Update/Redstone4)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.503
  [Host]     : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT


```
|                          Method |    Mean |    Error |   StdDev | Ratio |
|-------------------------------- |--------:|---------:|---------:|------:|
|     TestCoordinateArraySequence | 4.832 s | 0.0390 s | 0.0365 s |  1.00 |
|  TestCoordinateArraySequenceOpt | 5.550 s | 0.0347 s | 0.0290 s |  1.15 |
|     TestPackedDoubleSequenceOpt | 2.342 s | 0.0263 s | 0.0246 s |  0.48 |
| TestDotSpatialAffineSequenceOpt | 2.422 s | 0.0203 s | 0.0170 s |  0.50 |
|        TestPackedDoubleSequence | 2.758 s | 0.0272 s | 0.0254 s |  0.57 |
|    TestDotSpatialAffineSequence | 2.737 s | 0.0182 s | 0.0162 s |  0.57 |
