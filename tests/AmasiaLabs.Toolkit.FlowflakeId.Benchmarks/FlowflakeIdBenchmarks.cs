using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.FlowflakeId.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class FlowflakeIdBenchmarks : IAsyncDisposable
{
    private IFlowflakeId _local = null!;
    private IServiceProvider? _localSp;

    [Params(1, 4, 16, 64)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int Degree { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var localCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Amasia:Toolkit:FlowflakeId:InstanceId"] = "1",
                ["Amasia:Toolkit:FlowflakeId:UseUtcNow"] = "true",
                ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:Epoch"] = "2024-01-01T00:00:00Z",
                ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:TimeSemantics"] = "UtcNormalized",
            })
            .Build();

        var localServices = new ServiceCollection();
        localServices.AddSingleton<IConfiguration>(localCfg);
        localServices.AddFlowflakeId(localCfg);
        _localSp = localServices.BuildServiceProvider();
        _local = _localSp.GetRequiredService<IFlowflakeId>();

        _ = await _local.GenerateAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup() => await DisposeAsync();

    [Benchmark]
    public async Task<long> GenerateSingle_Local_Parallel()
    {
        var tasks = new Task<long>[Degree];
        for (int i = 0; i < Degree; i++)
            tasks[i] = _local.GenerateAsync().AsTask();

        var ids = await Task.WhenAll(tasks);
        return ids[0];
    }

    [Benchmark(Baseline = true)]
    public Task<long> GenerateSingle_Local() => _local.GenerateAsync().AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch10_Local() => _local.GenerateBatchAsync(10).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch100_Local() => _local.GenerateBatchAsync(100).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch1000_Local() => _local.GenerateBatchAsync(1000).AsTask();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_localSp is IAsyncDisposable ad1) return ad1.DisposeAsync();
        (_localSp as IDisposable)?.Dispose();
        return ValueTask.CompletedTask;
    }
}
