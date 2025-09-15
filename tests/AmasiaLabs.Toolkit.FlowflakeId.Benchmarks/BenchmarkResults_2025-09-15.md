# FlowflakeId Benchmark Results with Parallelism
**Date:** September 15, 2025
**Environment:** macOS Sequoia 15.6.1, Intel Core i9-9880H @ 2.30GHz, .NET 9.0.3

## Performance Summary

### Impact of Parallelism on Performance (Degree of Parallelism)

#### Single ID Generation
| Method | DOP=1 | DOP=4 | DOP=16 | DOP=64 |
|--------|-------|-------|--------|--------|
| **Local** | 54.99 ns | 54.52 ns | 68.41 ns | 49.11 ns |
| **Local Parallel** | 150.90 ns | 371.96 ns | 1,527.62 ns | 4,351.89 ns |
| **gRPC** | 155,281.78 ns | 152,006.10 ns | 171,263.21 ns | 141,487.08 ns |
| **gRPC Parallel** | 150,824.01 ns | 209,918.85 ns | 739,566.23 ns | 1,820,374.90 ns |

#### Batch 10 IDs
| Method | DOP=1 | DOP=4 | DOP=16 | DOP=64 |
|--------|-------|-------|--------|--------|
| **Local** | 462.55 ns | 462.89 ns | 475.85 ns | 425.79 ns |
| **gRPC** | 162,381.90 ns | 154,594.74 ns | 147,046.62 ns | 142,916.94 ns |

#### Batch 100 IDs
| Method | DOP=1 | DOP=4 | DOP=16 | DOP=64 |
|--------|-------|-------|--------|--------|
| **Local** | 4,329.89 ns | 4,261.98 ns | 4,351.94 ns | 3,954.09 ns |
| **gRPC** | 167,729.06 ns | 171,932.88 ns | 161,194.82 ns | 153,555.20 ns |

#### Batch 1000 IDs
| Method | DOP=1 | DOP=4 | DOP=16 | DOP=64 |
|--------|-------|-------|--------|--------|
| **Local** | 42,536.74 ns | 43,713.16 ns | 40,086.94 ns | 39,497.43 ns |
| **gRPC** | 275,035.12 ns | 282,349.41 ns | 253,297.73 ns | 253,021.85 ns |

## Key Insights

### Throughput Analysis (Correct Interpretation)

#### gRPC Parallel Throughput (requests/sec)
- **DOP=1**: ~6,600 req/s (150.8 μs total for 1 request)
- **DOP=4**: ~19,100 req/s (209.9 μs total for 4 requests = ~52.5 μs/req effective)
- **DOP=16**: ~21,600 req/s (739.6 μs total for 16 requests = ~46.2 μs/req effective)
- **DOP=64**: ~35,200 req/s (1,820.4 μs total for 64 requests = ~28.4 μs/req effective)

**Result**: Parallelism increases throughput by 5.3× from DOP=1 to DOP=64 ✅

### Cost Per ID Analysis

#### Local Generation
- **Single ID**: ~55 ns per ID
- **Batch 1000**: ~40-43 ns per ID (slightly better due to amortization)
- **Parallel overhead**: Not worth it - synchronization costs exceed benefits

#### gRPC Generation
- **Single ID**: ~150-170 μs per ID (network/serialization overhead)
- **Batch 10**: ~16 μs per ID (10× improvement)
- **Batch 100**: ~1.6 μs per ID (100× improvement)
- **Batch 1000**: ~253 ns per ID (almost local performance!)

### Memory Scaling
- **Sequential gRPC**: ~9 KB per request
- **Parallel gRPC at DOP=64**: ~588 KB total = ~9.2 KB per concurrent request
- **Conclusion**: Linear scaling as expected, not an explosion ✅

### Optimal Strategies

1. **For lowest latency per ID**: Use local generation (~55 ns)
2. **For high throughput over network**:
   - Use batches (100-1000 IDs per request)
   - Apply moderate parallelism (DOP=4-16 for balance)
3. **Best cost/benefit**: Batch 1000 with DOP=4-8 gives ~1M+ IDs/sec

## Raw Benchmark Data

```
BenchmarkDotNet v0.15.2, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Intel Core i9-9880H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0

| Method                        | Degree | Mean            | Error         | StdDev         | Median          | Ratio     | RatioSD  | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |------- |----------------:|--------------:|---------------:|----------------:|----------:|---------:|--------:|-------:|----------:|------------:|
| GenerateSingle_Grpc_Parallel  | 1      |   150,824.01 ns |  2,626.118 ns |   4,240.692 ns |   149,970.63 ns |  2,742.77 |    80.28 |  0.9766 |      - |    9414 B |      130.75 |
| GenerateSingle_Local_Parallel | 1      |       150.90 ns |      1.330 ns |       1.110 ns |       151.27 ns |      2.74 |     0.03 |  0.0391 |      - |     328 B |        4.56 |
| GenerateSingle_Local          | 1      |        54.99 ns |      0.604 ns |       0.535 ns |        54.96 ns |      1.00 |     0.01 |  0.0086 |      - |      72 B |        1.00 |
| GenerateSingle_Grpc           | 1      |   155,281.78 ns |  2,073.012 ns |   2,695.501 ns |   154,920.34 ns |  2,823.84 |    54.86 |  0.9766 |      - |    9126 B |      126.75 |
| GenerateBatch10_Local         | 1      |       462.55 ns |      3.212 ns |       2.848 ns |       462.28 ns |      8.41 |     0.09 |  0.0210 |      - |     176 B |        2.44 |
| GenerateBatch10_Grpc          | 1      |   162,381.90 ns |  3,217.170 ns |   5,963.230 ns |   161,946.58 ns |  2,952.96 |   110.78 |  0.9766 |      - |    9861 B |      136.96 |
| GenerateBatch100_Local        | 1      |     4,329.89 ns |     51.577 ns |      43.069 ns |     4,307.91 ns |     78.74 |     1.06 |  0.1068 |      - |     896 B |       12.44 |
| GenerateBatch100_Grpc         | 1      |   167,729.06 ns |  2,798.557 ns |   3,539.279 ns |   167,833.82 ns |  3,050.20 |    69.23 |  1.4648 |      - |   13887 B |      192.88 |
| GenerateBatch1000_Local       | 1      |    42,536.74 ns |    404.114 ns |     378.008 ns |    42,539.73 ns |    773.54 |     9.85 |  0.9155 |      - |    8096 B |      112.44 |
| GenerateBatch1000_Grpc        | 1      |   275,035.12 ns |  4,887.935 ns |   7,316.031 ns |   272,795.97 ns |  5,001.58 |   139.10 |  5.8594 |      - |   49902 B |      693.08 |
|                               |        |                 |               |                |                 |           |          |         |        |           |             |
| GenerateSingle_Grpc_Parallel  | 4      |   209,918.85 ns |  3,943.247 ns |   6,478.869 ns |   208,432.64 ns |  3,850.43 |   119.77 |  3.9063 |      - |   36503 B |      506.99 |
| GenerateSingle_Local_Parallel | 4      |       371.96 ns |      4.694 ns |       4.390 ns |       371.12 ns |      6.82 |     0.09 |  0.0734 |      - |     616 B |        8.56 |
| GenerateSingle_Local          | 4      |        54.52 ns |      0.382 ns |       0.357 ns |        54.54 ns |      1.00 |     0.01 |  0.0086 |      - |      72 B |        1.00 |
| GenerateSingle_Grpc           | 4      |   152,006.10 ns |  2,034.564 ns |   2,498.628 ns |   152,122.89 ns |  2,788.16 |    48.22 |  0.9766 |      - |    9126 B |      126.75 |
| GenerateBatch10_Local         | 4      |       462.89 ns |      3.073 ns |       2.874 ns |       463.50 ns |      8.49 |     0.07 |  0.0210 |      - |     176 B |        2.44 |
| GenerateBatch10_Grpc          | 4      |   154,594.74 ns |  2,357.395 ns |   2,895.093 ns |   153,787.23 ns |  2,835.65 |    55.00 |  0.9766 |      - |    9862 B |      136.97 |
| GenerateBatch100_Local        | 4      |     4,261.98 ns |     31.393 ns |      29.365 ns |     4,249.63 ns |     78.18 |     0.72 |  0.1068 |      - |     896 B |       12.44 |
| GenerateBatch100_Grpc         | 4      |   171,932.88 ns |  1,980.049 ns |   2,643.308 ns |   171,490.60 ns |  3,153.67 |    51.62 |  1.4648 |      - |   13882 B |      192.81 |
| GenerateBatch1000_Local       | 4      |    43,713.16 ns |    866.411 ns |   2,236.483 ns |    43,262.19 ns |    801.81 |    41.09 |  0.9155 |      - |    8096 B |      112.44 |
| GenerateBatch1000_Grpc        | 4      |   282,349.41 ns |  5,626.324 ns |   7,510.981 ns |   280,020.63 ns |  5,178.98 |   139.12 |  5.8594 |      - |   49902 B |      693.08 |
|                               |        |                 |               |                |                 |           |          |         |        |           |             |
| GenerateSingle_Grpc_Parallel  | 16     |   739,566.23 ns | 41,487.838 ns | 119,701.906 ns |   716,019.50 ns | 10,818.23 | 1,767.79 | 17.5781 | 1.9531 |  147771 B |    2,052.38 |
| GenerateSingle_Local_Parallel | 16     |     1,527.62 ns |     15.707 ns |      31.004 ns |     1,531.16 ns |     22.35 |     0.76 |  0.2098 |      - |    1768 B |       24.56 |
| GenerateSingle_Local          | 16     |        68.41 ns |      1.402 ns |       1.823 ns |        68.84 ns |      1.00 |     0.04 |  0.0086 |      - |      72 B |        1.00 |
| GenerateSingle_Grpc           | 16     |   171,263.21 ns |  3,390.407 ns |   7,857.783 ns |   173,612.38 ns |  2,505.20 |   133.11 |  0.9766 |      - |    9126 B |      126.75 |
| GenerateBatch10_Local         | 16     |       475.85 ns |      9.607 ns |      16.826 ns |       479.94 ns |      6.96 |     0.31 |  0.0210 |      - |     176 B |        2.44 |
| GenerateBatch10_Grpc          | 16     |   147,046.62 ns |  2,106.667 ns |   2,883.630 ns |   146,470.77 ns |  2,150.97 |    71.96 |  0.9766 |      - |    9860 B |      136.94 |
| GenerateBatch100_Local        | 16     |     4,351.94 ns |     84.010 ns |     120.485 ns |     4,329.13 ns |     63.66 |     2.46 |  0.1068 |      - |     896 B |       12.44 |
| GenerateBatch100_Grpc         | 16     |   161,194.82 ns |  3,209.247 ns |   4,704.073 ns |   161,662.49 ns |  2,357.93 |    93.51 |  1.4648 |      - |   13885 B |      192.85 |
| GenerateBatch1000_Local       | 16     |    40,086.94 ns |    756.100 ns |     956.224 ns |    39,794.88 ns |    586.38 |    21.10 |  0.9155 |      - |    8096 B |      112.44 |
| GenerateBatch1000_Grpc        | 16     |   253,297.73 ns |  4,474.378 ns |   5,973.166 ns |   253,788.72 ns |  3,705.19 |   132.76 |  5.8594 |      - |   49898 B |      693.03 |
|                               |        |                 |               |                |                 |           |          |         |        |           |             |
| GenerateSingle_Grpc_Parallel  | 64     | 1,820,374.90 ns | 47,437.020 ns | 133,018.501 ns | 1,858,658.16 ns | 37,068.87 | 2,705.76 | 70.3125 | 7.8125 |  588432 B |    8,172.67 |
| GenerateSingle_Local_Parallel | 64     |     4,351.89 ns |     77.224 ns |      68.457 ns |     4,323.95 ns |     88.62 |     1.47 |  0.7553 |      - |    6376 B |       88.56 |
| GenerateSingle_Local          | 64     |        49.11 ns |      0.376 ns |       0.333 ns |        49.16 ns |      1.00 |     0.01 |  0.0086 |      - |      72 B |        1.00 |
| GenerateSingle_Grpc           | 64     |   141,487.08 ns |  2,757.711 ns |   4,374.022 ns |   141,239.20 ns |  2,881.15 |    89.81 |  0.9766 |      - |    9124 B |      126.72 |
| GenerateBatch10_Local         | 64     |       425.79 ns |      5.073 ns |       4.746 ns |       426.29 ns |      8.67 |     0.11 |  0.0210 |      - |     176 B |        2.44 |
| GenerateBatch10_Grpc          | 64     |   142,916.94 ns |  2,104.881 ns |   2,809.956 ns |   142,831.94 ns |  2,910.26 |    59.29 |  0.9766 |      - |    9863 B |      136.99 |
| GenerateBatch100_Local        | 64     |     3,954.09 ns |     27.047 ns |      23.977 ns |     3,946.97 ns |     80.52 |     0.71 |  0.1068 |      - |     896 B |       12.44 |
| GenerateBatch100_Grpc         | 64     |   153,555.20 ns |  2,096.032 ns |   2,495.177 ns |   153,626.87 ns |  3,126.89 |    53.72 |  1.4648 |      - |   13888 B |      192.89 |
| GenerateBatch1000_Local       | 64     |    39,497.43 ns |    405.863 ns |     379.644 ns |    39,407.96 ns |    804.30 |     9.15 |  0.9155 |      - |    8096 B |      112.44 |
| GenerateBatch1000_Grpc        | 64     |   253,021.85 ns |  4,525.122 ns |   5,557.256 ns |   252,689.11 ns |  5,152.36 |   115.77 |  5.8594 |      - |   49900 B |      693.06 |
```

### Notes
- Outliers were removed from measurements for statistical accuracy
- All times in nanoseconds (ns), 1 ns = 0.000000001 sec
- Memory measured per operation in bytes
- Gen0/Gen1 represents garbage collections per 1000 operations
- Degree = Degree of Parallelism (DOP) used in parallel tests