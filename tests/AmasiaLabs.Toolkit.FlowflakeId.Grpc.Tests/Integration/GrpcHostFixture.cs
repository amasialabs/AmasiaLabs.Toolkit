using System.Net;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Tests.Integration;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GrpcHostFixture : IAsyncLifetime
{
    public const int InstanceId = 5;
    public static readonly DateTime EpochUtc = new(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
    public const FlowflakeTimeSemantics Semantics = FlowflakeTimeSemantics.UtcNormalized;

    // ReSharper disable once MemberCanBePrivate.Global
    public WebApplication App { get; private set; } = null!;
    // ReSharper disable once MemberCanBePrivate.Global
    public string Address { get; private set; } = null!;
    // ReSharper disable once MemberCanBePrivate.Global
    public IServiceProvider ClientServices { get; private set; } = null!;
    public IFlowflakeId Client { get; private set; } = null!;

    static IConfiguration BuildServerConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Amasia:Toolkit:FlowflakeId:InstanceId"] = InstanceId.ToString(),
                ["Amasia:Toolkit:FlowflakeId:UseUtcNow"] = "true",
                ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:Epoch"] = EpochUtc.ToString("O"),
                ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:TimeSemantics"] = Semantics.ToString(),
            })
            .Build();
    public async ValueTask InitializeAsync()
    {
        var cfg = BuildServerConfig();

        var b = WebApplication.CreateBuilder();
        b.WebHost.UseKestrel(o => o.Listen(IPAddress.Loopback, 0, lo => lo.Protocols = HttpProtocols.Http2));
        b.Services.AddFlowflakeId(cfg);
        b.Services.AddGrpc();

        App = b.Build();
        App.MapFlowflakeIdGrpc();
        await App.StartAsync();

        Address = GetBoundAddress(App);

        var clientCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:Addresses:0"] = Address,
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:DeadlineMs"] = "5000",
                ["Amasia:Toolkit:FlowflakeId:Grpc:Client:MaxAttempts"] = "1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(clientCfg);
        services.AddFlowflakeIdGrpcClient(clientCfg);

        ClientServices = services.BuildServiceProvider();
        Client = ClientServices.GetRequiredService<IFlowflakeId>();

        _ = await Client.GenerateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    
        if (ClientServices is IAsyncDisposable disposableClientProvider)
        {
            await disposableClientProvider.DisposeAsync();
        }
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