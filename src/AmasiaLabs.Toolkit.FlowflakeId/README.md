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
        "Epoch": "2023-02-15T00:00:00Z",
        "TimeSemantics": "UtcNormalized"
      }
    }
  }
}
```

You can also bind from any custom section path by passing it to `AddFlowflakeId(configuration, sectionPath)`.

## ID Layout (compat mode)

- Bits 31..63: seconds since epoch (you must set `Epoch` explicitly)
- Bits 22..30: instance id (1..511)
- Bits 0..21: sequence (internally capped to 2^21-1 for compatibility)

Defaults mirror modern, UTC-normalized behavior (safer for distributed systems). To preserve legacy behavior, set `TimeSemantics = LegacyUnspecifiedEpoch`.

## Usage

```csharp
using AmasiaLabs.Toolkit.FlowflakeId;

var builder = Host.CreateApplicationBuilder(args);

// Default path: "Amasia:Toolkit:FlowflakeId"
// TimeProvider is registered as TimeProvider.System by default.
builder.Services.AddFlowflakeId(builder.Configuration);

// Or bind from a custom path
builder.Services.AddFlowflakeId(builder.Configuration, sectionPath: "My:Custom:Section");

// Or configure in code only (no config binding)
builder.Services.AddFlowflakeId(o =>
{
    o.InstanceId = 1;
    o.UseUtcNow = true;
    o.Epoch = new DateTime(2023, 02, 15);
});

var app = builder.Build();

var ids = app.Services.GetRequiredService<IFlowflakeId>();
var id = ids.Generate();
```

### API

```csharp
public interface IFlowflakeId
{
    long Generate();
    long GenerateForDate(DateTime date);
    int GetInstanceId();
    int GetInstanceIdFromGlobalId(long id);
    DateTime GetDateTime(long id);
    string ToBase62(long id);
    long FromBase62(string id);
}

### Testing time-dependent behavior

Inject a custom `TimeProvider` (e.g., `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`) to control time in tests:

```csharp
var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
var fake = new FakeTimeProvider(new DateTimeOffset(epoch).AddSeconds(10));
var options = Options.Create(new FlowflakeIdOptions { InstanceId = 1, UseUtcNow = true, Epoch = epoch });
var gen = new FlowflakeId(options, new NumericBase62Codec(), fake);

var id1 = gen.Generate();
fake.Advance(TimeSpan.FromSeconds(1));
var id2 = gen.Generate();
```

## Time Semantics

- `UtcNormalized` (default):
  - Epoch and input times are normalized to UTC before computing seconds.
  - Recommended for services (e.g., gRPC) and multi-zone deployments.
- `LegacyUnspecifiedEpoch`:
  - Seconds are computed as a raw `DateTime` difference without UTC normalization.
  - Preserves historical behavior when `Epoch` was `Unspecified` and `Generate()` used `DateTime.Now`.
  - Use only to maintain exact continuity of existing IDs; prefer `UtcNormalized` for new systems.
```

### Base62

Base62 codec is pluggable. The default codec performs numeric base62 encode/decode for long values. If you need compatibility with a specific external Base62 format, register your own `IBase62Codec` in DI before calling `AddFlowflakeId`.
