using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Flowflake options: default path "Amasia:Toolkit:FlowflakeId"
builder.Services.AddFlowflakeId(builder.Configuration);

// gRPC
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
// Optional: bind server options (e.g., MaxBatchSize) from configuration
builder.Services.AddOptionsWithValidateOnStart<FlowflakeIdServerOptions>()
    .Bind(builder.Configuration.GetSection(FlowflakeIdServerOptions.DefaultSectionPath))
    .ValidateDataAnnotations();

var app = builder.Build();

app.MapFlowflakeIdGrpc();
app.MapGrpcHealthChecksService();

app.Run();
