using AmasiaLabs.Toolkit.FlowflakeId.Grpc.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.AddFlowflakeServer();
builder.Services.AddGrpcHealthChecks();

var app = builder.Build();

app.MapFlowflakeServer();
app.MapGrpcHealthChecksService();

app.Run();
