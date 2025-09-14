# AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client

Client SDK for the Flowflake ID gRPC service. It implements `IFlowflakeId`, handles failover across multiple addresses, caches `GetServerInfo`, and decodes timestamps/instance locally.

## Install

```
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client
```

## Configuration

Default section path: `Amasia:Toolkit:FlowflakeId:Rpc`.

```json
{
  "Amasia": {
    "Toolkit": {
      "FlowflakeId": {
        "Rpc": {
          "Addresses": [ "http://localhost:8080" ],
          "DeadlineMs": 200,
          "MaxAttempts": 2
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

Decode helpers (`GetDateTime`, `GetInstanceIdFromGlobalId`) are synchronous and rely on cached `ServerInfo` (epoch/layout). Call any async method (e.g., `GenerateAsync`) once to initialize before using them. For text formatting/parsing, use an `IIdCodec` (e.g., `NumericBase62Codec`) and the `FormatId`/`ParseId` extensions.

## Backlog

- Segment/batch leases RPC to minimize per-ID RPCs and allow local consumption.
- Hedging/retries via gRPC ServiceConfig; optional static resolver with round-robin.
- mTLS and cert pinning options.
- ServerInfo TTL and background refresh with jitter.
- Optional async interface for non-blocking RPC calls.
