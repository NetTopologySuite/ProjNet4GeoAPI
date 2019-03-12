``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.590 (1803/April2018Update/Redstone4)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.503
  [Host]     : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT


```
|                       Method |    Mean |    Error |   StdDev | Ratio |
|----------------------------- |--------:|---------:|---------:|------:|
|  TestCoordinateArraySequence | 4.823 s | 0.0431 s | 0.0360 s |  1.00 |
|     TestPackedDoubleSequence | 2.629 s | 0.0406 s | 0.0360 s |  0.55 |
| TestDotSpatialAffineSequence | 2.721 s | 0.0219 s | 0.0183 s |  0.56 |
