# AmasiaLabs.Toolkit.FlowflakeId

Snowflake-like unique ID generator with configurable epoch and time source, preserving compatibility with an existing seconds-based layout.

## Install

```bash
dotnet add package AmasiaLabs.Toolkit.FlowflakeId
```

## Configuration

Default section path: `Amasia:Toolkit:FlowflakeId`.

appsettings.json:

```json
{
  "Amasia": {
    "Toolkit": {
      "FlowflakeId": {
        "InstanceId": 1,
        "UseUtcNow": true,
        "FlowflakeClock": {
          "Epoch": "2023-02-15T00:00:00Z",
          "TimeSemantics": "UtcNormalized"
        }
      }
    }
  }
}
```

You can also bind from any custom section path by passing it to `AddFlowflakeId(configuration, sectionPath)`.

## ID Layout (compat mode)

- Bits 31..63: seconds since epoch (you must set `Epoch` explicitly)
- Bits 22..30: instance id (1..511)
- Bits 0..21: sequence (22 bits, max 2^22-1)

Defaults mirror modern, UTC-normalized behavior (safer for distributed systems). To preserve legacy behavior, set `TimeSemantics = LegacyUnspecifiedEpoch`.

## Usage

```csharp
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Simplest - uses IConfiguration from DI, reads from "Amasia:Toolkit:FlowflakeId"
builder.Services.AddFlowflakeId();

// Or with additional configuration
builder.Services.AddFlowflakeId(o => o.FailoverInstanceId = 999);

// Or bind from explicit configuration
builder.Services.AddFlowflakeId(builder.Configuration);

// Or bind from a custom path
builder.Services.AddFlowflakeId(builder.Configuration, sectionPath: "My:Custom:Section");

// Or configure in code only (no config binding)
builder.Services.AddFlowflakeId(o =>
{
    o.InstanceId = 1;
    o.UseUtcNow = true;
    o.FlowflakeClock = new FlowflakeClockOptions
    {
        Epoch = new DateTime(2023, 02, 15),
        TimeSemantics = FlowflakeTimeSemantics.UtcNormalized
    };
});

var app = builder.Build();

var ids = app.Services.GetRequiredService<IFlowflakeId>();
var id = await ids.GenerateAsync();
```

### API

```csharp
public interface IFlowflakeId
{
    ValueTask<long> GenerateAsync(CancellationToken cancellationToken = default);
    ValueTask<long> GenerateForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    ValueTask<long[]> GenerateBatchAsync(int size, CancellationToken cancellationToken = default);
    ValueTask<long[]> GenerateBatchForDateAsync(DateTime date, int size, CancellationToken cancellationToken = default);

    int InstanceId { get; }  // Property to get the configured instance ID
}
```

### Extracting Information from IDs

```csharp
// Using extension methods from AmasiaLabs.Toolkit.FlowflakeId.Extensions
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

var id = await generator.GenerateAsync();

// Extract components
int instanceId = id.GetInstanceIdFromFlowflakeId();
int sequenceNumber = id.GetSequenceNumberFromFlowflakeId();
long timestamp = id.GetTimestampFromFlowflakeId(); // seconds since epoch

// Extract DateTime using configured clock
var options = app.Services.GetRequiredService<IOptions<FlowflakeIdOptions>>();
DateTime dateTime = id.GetDateTimeFromFlowflakeId(options.Value.ToFlowflakeClock());

// Or if you have FlowflakeClockOptions directly (for decode-only scenarios)
var clockOptions = app.Services.GetRequiredService<IOptions<FlowflakeClockOptions>>();
DateTime dateTime = id.GetDateTimeFromFlowflakeId(clockOptions.Value.ToFlowflakeClock());
```

### Testing time-dependent behavior

Inject a custom `TimeProvider` (e.g., `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`) to control time in tests:

```csharp
var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
var fake = new FakeTimeProvider(new DateTimeOffset(epoch).AddSeconds(10));
var options = Options.Create(new FlowflakeIdOptions
{
    InstanceId = 1,
    UseUtcNow = true,
    FlowflakeClock = new FlowflakeClockOptions
    {
        Epoch = epoch,
        TimeSemantics = FlowflakeTimeSemantics.UtcNormalized
    }
});
var gen = new FlowflakeId(options, fake);

var id1 = await gen.GenerateAsync();
fake.Advance(TimeSpan.FromSeconds(1));
var id2 = await gen.GenerateAsync();
```

## Decode-Only Scenarios

For services that only need to decode DateTime from existing IDs without generating new ones:

```csharp
// Simplest - uses IConfiguration from DI
builder.Services.AddFlowflakeClock();

// Or with explicit configuration
builder.Services.AddFlowflakeClock(builder.Configuration);

// Usage
var clockOptions = app.Services.GetRequiredService<IOptions<FlowflakeClockOptions>>();
DateTime dateTime = id.GetDateTimeFromFlowflakeId(clockOptions.Value.ToFlowflakeClock());
```

Configuration for decode-only:
```json
{
  "Amasia": {
    "Toolkit": {
      "FlowflakeId": {
        "FlowflakeClock": {
          "Epoch": "2023-02-15T00:00:00Z",
          "TimeSemantics": "UtcNormalized"
        }
      }
    }
  }
}
```

## Time Semantics

- `UtcNormalized` (default):
  - Epoch and input times are normalized to UTC before computing seconds.
  - Recommended for services (e.g., gRPC) and multi-zone deployments.
- `LegacyUnspecifiedEpoch`:
  - Seconds are computed as a raw `DateTime` difference without UTC normalization.
  - Preserves historical behavior when `Epoch` was `Unspecified` and `Generate()` used `DateTime.Now`.
  - Use only to maintain exact continuity of existing IDs; prefer `UtcNormalized` for new systems.

### Formatting (Codecs)

Formatting/parsing of IDs is decoupled from `IFlowflakeId`.

- Use `IIdCodec` to plug different formats (default: `NumericBase62Codec`).
- Extension helpers are available: `FormatFlowflakeId` and `ParseFlowflakeId`.

```csharp
var codec = new NumericBase62Codec();
var id = await gen.GenerateAsync();

// Using the codec directly
string text = codec.Encode(id);
long back = codec.Decode(text);

// Or via extensions
string text2 = id.FormatFlowflakeId(codec);
long back2 = text2.ParseFlowflakeId(codec);
```

## Backlog

- gRPC client SDK: segment/lease RPC to allow local ID generation between refreshes.
- Optional async-first `IFlowflakeId` variant for non-blocking remote calls.
- Full-local mode bootstrap (allocate free instance id from a coordinator). Not planned yet.
