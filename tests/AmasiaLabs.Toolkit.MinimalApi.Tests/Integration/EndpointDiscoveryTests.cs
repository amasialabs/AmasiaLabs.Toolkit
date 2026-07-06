using System.Net;
using System.Reflection;
using AmasiaLabs.Toolkit.MinimalApi.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class EndpointDiscoveryTests
{
    [Fact]
    public void AddEndpoints_Generic_Marker_Registers_Declared_Services()
    {
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder().Build();

        services.AddEndpoints<MarkerEndpoint>(config);

        services.Any(d => d.ServiceType == typeof(IProbeService)).Should().BeTrue();
    }

    [Fact]
    public void AddEndpoints_With_Explicit_Assembly_Registers_Declared_Services()
    {
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder().Build();

        services.AddEndpoints(config, typeof(MarkerEndpoint).Assembly);

        services.Any(d => d.ServiceType == typeof(IProbeService)).Should().BeTrue();
    }

    [Fact]
    public async Task UseEndpoints_Generic_Marker_Maps_Declared_Routes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddEndpoints<MarkerEndpoint>(builder.Configuration);

        await using var app = builder.Build();
        app.UseEndpoints<MarkerEndpoint>();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var resp = await client.GetAsync("/__probe", TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("probe-ok");
    }

    [Fact]
    public async Task UseEndpoints_Maps_Routes_Declared_Via_Explicit_Interface_Implementation()
    {
        // ExplicitEndpoint implements the static-abstract DefineEndPoints via an EXPLICIT interface
        // implementation, which is private and name-mangled in metadata. A plain GetMethod misses
        // it, so discovery must fall back to the interface map — otherwise the route is silently
        // never mapped (a 404 with no diagnostic).
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        await using var app = builder.Build();
        app.UseEndpoints(typeof(ExplicitEndpoint).Assembly);
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var resp = await client.GetAsync("/__probe-explicit", TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("explicit-ok");
    }

    [Fact]
    public void Calling_Assembly_Resolving_Overloads_Must_Be_NoInlining()
    {
        // The parameterless AddEndpoints/UseEndpoints overloads resolve the caller's assembly via
        // Assembly.GetCallingAssembly(), which is only reliable when the capturing method is not
        // inlined. This guards the discipline so a future overload cannot silently ship without
        // [MethodImpl(MethodImplOptions.NoInlining)] and regress to scanning the toolkit assembly.
        var resolvingOverloads = typeof(EndpointExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "AddEndpoints" or "UseEndpoints"
                        && !m.IsGenericMethod
                        && m.GetParameters().All(p => p.ParameterType != typeof(Assembly)))
            .ToList();

        resolvingOverloads.Should().NotBeEmpty();
        foreach (var method in resolvingOverloads)
            method.MethodImplementationFlags.Should().HaveFlag(MethodImplAttributes.NoInlining,
                $"{method} resolves the calling assembly and must not be inlined");
    }

    // --- test fixtures ---

    public interface IProbeService;

    private sealed class ProbeService : IProbeService;

    // Implements IEndpoints with BOTH members provided.
    public sealed class MarkerEndpoint : IEndpoints
    {
        public static void AddEndPointServices(IServiceCollection services, IConfiguration configuration)
            => services.AddSingleton<IProbeService, ProbeService>();

        public static void DefineEndPoints(IEndpointRouteBuilder app)
            => app.MapGet("/__probe", () => Results.Text("probe-ok"));
    }

    // Implements IEndpoints but relies on the default no-op AddEndPointServices; discovery must
    // skip the absent override gracefully (exercised by every assembly-scanning test above).
    public sealed class NoServicesEndpoint : IEndpoints
    {
        public static void DefineEndPoints(IEndpointRouteBuilder app)
            => app.MapGet("/__probe2", () => Results.Ok());
    }

    // Implements the required member via an EXPLICIT interface implementation.
    public sealed class ExplicitEndpoint : IEndpoints
    {
        static void IEndpoints.DefineEndPoints(IEndpointRouteBuilder app)
            => app.MapGet("/__probe-explicit", () => Results.Text("explicit-ok"));
    }
}
