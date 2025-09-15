using AmasiaLabs.Toolkit.FlowflakeId.Grpc.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddFlowflakeServer();
builder.Services.AddGrpcHealthChecks();

var app = builder.Build();

app.MapFlowflakeServer();
app.MapGrpcHealthChecksService();

app.Run();
