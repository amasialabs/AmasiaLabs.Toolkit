using AmasiaLabs.Toolkit.FlowflakeId;
using AmasiaLabs.Toolkit.FlowflakeId.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Kestrel: enable HTTP/2 without TLS on port 8080 (h2c)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Flowflake options: default path "Amasia:Toolkit:FlowflakeId"
builder.Services.AddFlowflakeId(builder.Configuration);

// gRPC
builder.Services.AddGrpc();

var app = builder.Build();

app.MapFlowflakeIdGrpc();

// Optional informational endpoint
app.MapGet("/", () => "FlowflakeId gRPC Service. Use a gRPC client to communicate.");

app.Run();

