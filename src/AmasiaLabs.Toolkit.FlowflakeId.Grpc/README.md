# AmasiaLabs.Toolkit.FlowflakeId.Grpc

gRPC service wrapper for the Flowflake ID generator (`IFlowflakeId`).

## Install

```
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Grpc
```

## Usage

```
var builder = WebApplication.CreateBuilder(args);

// Configure Flowflake options (preferred config path: "Amasia:Toolkit:FlowflakeId")
builder.Services.AddFlowflakeId(builder.Configuration);

// Add gRPC
builder.Services.AddGrpc();

var app = builder.Build();
app.MapFlowflakeIdGrpc();
app.Run();
```

### RPCs

- GetId(Empty) → IdResponse
- GetIdForDate(DateRequest) → IdResponse
- GetServerInfo(Empty) → ServerInfo

ServerInfo exposes epoch, UseUtcNow flag, instance ids, and the bit layout, so clients can implement local decoding and formatting (e.g., Base62) without additional network calls.

