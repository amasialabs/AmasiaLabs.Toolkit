# AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client

Client SDK for the Flowflake ID gRPC service. It implements `IFlowflakeId`, handles failover across multiple addresses, caches `GetServerInfo`, and decodes timestamps/instance locally.

## Install

```
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client
```

## Configuration

Default section path: `Amasia:Toolkit:FlowflakeId:Grpc:Client`.

```json
{
  "Amasia": {
    "Toolkit": {
      "FlowflakeId": {
        "Grpc": {
          "Client": {
            "Addresses": [ "http://localhost:8080" ],
            "DeadlineMs": 200,
            "MaxAttempts": 2
          }
        }
      }
    }
  }
}
```

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register gRPC client for IFlowflakeId
builder.Services.AddFlowflakeIdGrpcClient(builder.Configuration);

var app = builder.Build();

var ids = app.Services.GetRequiredService<IFlowflakeId>();
var id = await ids.GenerateAsync();
var batch = await ids.GenerateBatchAsync(10);

app.Run();
```

Note: Do not register both the local generator (`AddFlowflakeId`) and the gRPC client at the same time for the same `IFlowflakeId`.

DateTime extraction and component parsing use extension methods from `AmasiaLabs.Toolkit.FlowflakeId.Extensions`. The gRPC client caches `ServerInfo` (epoch/layout) from the server - call any async method (e.g., `GenerateAsync`) once to initialize before using decode extensions. For text formatting/parsing, use the extensions from `FlowflakeId.Extensions` package.

## Backlog

- Segment/batch leases RPC to minimize per-ID RPCs and allow local consumption.
- Hedging/retries via gRPC ServiceConfig; optional static resolver with round-robin.
- mTLS and cert pinning options.
- ServerInfo TTL and background refresh with jitter.
- Optional async interface for non-blocking RPC calls.
