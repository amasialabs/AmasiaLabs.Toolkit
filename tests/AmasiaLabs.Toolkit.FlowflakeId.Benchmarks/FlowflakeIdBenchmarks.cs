using System.Net;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Grpc;   
using AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AmasiaLabs.Toolkit.FlowflakeId.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class FlowflakeIdBenchmarks : IAsyncDisposable
{
    private IFlowflakeId _local = null!;
    private IFlowflakeId _grpc  = null!;
    private IServiceProvider? _localSp;
    private IServiceProvider? _grpcSp;
    private WebApplication? _server;
    
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

        var b = WebApplication.CreateBuilder();
        b.Logging.ClearProviders();
        b.WebHost.UseKestrel(o => o.Listen(IPAddress.Loopback,0, lo => lo.Protocols = HttpProtocols.Http2));
        b.Services.AddFlowflakeId(localCfg);
        b.Services.AddGrpc();

        _server = b.Build();
        _server.MapFlowflakeIdGrpc();
        await _server.StartAsync();

        var address = GetBoundAddress(_server);

        var grpcCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:Addresses:0"] = address,
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:DeadlineMs"] = "5000",
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:MaxAttempts"] = "1",
            })
            .Build();

        var grpcServices = new ServiceCollection();
        grpcServices.AddSingleton<IConfiguration>(grpcCfg);
        grpcServices.AddFlowflakeIdGrpcClient(grpcCfg);
        _grpcSp = grpcServices.BuildServiceProvider();
        _grpc = _grpcSp.GetRequiredService<IFlowflakeId>();
        
        _ = await _local.GenerateAsync();
        _ = await _grpc.GenerateAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup() => await DisposeAsync();

    // ————— Benchmarks —————
    [Benchmark]
    public async Task<long> GenerateSingle_Grpc_Parallel()
    {
        var tasks = new Task<long>[Degree];
        for (int i = 0; i < Degree; i++)
            tasks[i] = _grpc.GenerateAsync().AsTask();

        var ids = await Task.WhenAll(tasks);
        return ids[0]; // BDN любит возвращаемое значение; берём любой
    }

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
    public Task<long> GenerateSingle_Grpc() => _grpc.GenerateAsync().AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch10_Local() => _local.GenerateBatchAsync(10).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch10_Grpc() => _grpc.GenerateBatchAsync(10).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch100_Local() => _local.GenerateBatchAsync(100).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch100_Grpc() => _grpc.GenerateBatchAsync(100).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch1000_Local() => _local.GenerateBatchAsync(1000).AsTask();

    [Benchmark]
    public Task<long[]> GenerateBatch1000_Grpc() => _grpc.GenerateBatchAsync(1000).AsTask();

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_server is not null)
        {
            await _server.StopAsync();
            await _server.DisposeAsync();
            _server = null;
        }

        if (_localSp is IAsyncDisposable ad1) await ad1.DisposeAsync(); else (_localSp as IDisposable)?.Dispose();
        if (_grpcSp  is IAsyncDisposable ad2) await ad2.DisposeAsync(); else (_grpcSp  as IDisposable)?.Dispose();
    }
    
    private static string GetBoundAddress(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        var feature = server.Features.Get<IServerAddressesFeature>();
        if (feature is null || feature.Addresses.Count == 0)
            throw new InvalidOperationException("No bound addresses. Make sure Kestrel is used and app is started.");

        return feature.Addresses.First();
    }
}
