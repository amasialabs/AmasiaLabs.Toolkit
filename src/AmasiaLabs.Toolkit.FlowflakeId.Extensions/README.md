# AmasiaLabs.Toolkit.FlowflakeId.Extensions

Dependency injection and formatting extensions for the Flowflake ID generator.

## Overview

This package provides:
- DI integration via `ServiceCollectionExtensions` (including decode-only scenarios)
- DateTime extraction via `FlowflakeIdParsingExtensions`
- Formatting helpers via `FlowflakeIdFormattingExtensions`
- Component parsing helpers via `FlowflakeIdParsingExtensions`
- Built-in codec implementations:
  - **Base36** - Alphanumeric encoding (0-9, a-z)
  - **Base58** - Bitcoin-style encoding (excludes ambiguous characters)
  - **Base62** - Alphanumeric encoding (0-9, a-z, A-Z)
  - **Base64Url** - URL-safe Base64 encoding
  - **Bech32** - Bech32/Bech32m encoding with built-in checksum (requires configuration)
  - **CrockfordBase32** - Douglas Crockford's Base32 encoding
  - **Hex** - Hexadecimal encoding

## Installation

```bash
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Extensions
```

## Usage

### Dependency Injection

#### Full ID Generation (with InstanceId)

```csharp
// Simplest - uses IConfiguration from DI, reads from "Amasia:Toolkit:FlowflakeId"
services.AddFlowflakeId();

// With additional configuration
services.AddFlowflakeId(options =>
{
    options.FailoverInstanceId = 999; // Override some settings
});

// Using explicit configuration object
services.AddFlowflakeId(configuration);

// Using configuration section
services.AddFlowflakeId(configuration.GetSection("My:Custom:Section"));

// Using only code-based configuration
services.AddFlowflakeId(options =>
{
    options.InstanceId = 1;
    options.UseUtcNow = true;
    options.FlowflakeClock = new FlowflakeClockOptions
    {
        Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        TimeSemantics = FlowflakeTimeSemantics.UtcNormalized
    };
});
```

Configuration format:
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

#### Decode-Only Scenarios (no InstanceId required)

For services that only need to decode DateTime from existing IDs without generating new ones:

```csharp
// Simplest - uses IConfiguration from DI, reads from "Amasia:Toolkit:FlowflakeId:FlowflakeClock"
services.AddFlowflakeClock();

// Using explicit configuration object
services.AddFlowflakeClock(configuration);

// Or with explicit section
services.AddFlowflakeClock(configuration.GetSection("My:Custom:Section"));
```

Configuration format for decode-only:
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

Usage example:
```csharp
// Get the clock options from DI
var clockOptions = serviceProvider.GetRequiredService<IOptions<FlowflakeClockOptions>>();

// Extract DateTime from any Flowflake ID
long existingId = 123456789L;
var dateTime = existingId.GetDateTimeFromFlowflakeId(clockOptions.Value.ToFlowflakeClock());

// Also works with other parsing extensions
var instanceId = existingId.GetInstanceIdFromFlowflakeId();
var sequence = existingId.GetSequenceNumberFromFlowflakeId();
var timestamp = existingId.GetTimestampFromFlowflakeId();
```

This is useful for:
- Microservices that receive IDs from other services
- Analytics/reporting services that need to extract timestamps
- Audit/logging systems that need to decode ID components
- Any service that consumes but doesn't generate Flowflake IDs

### Formatting Extensions

```csharp
var id = 123456789L;

// Using built-in codecs with enum (Base62 is default)
string base62 = id.FormatFlowflakeId(); // Uses Base62 by default
string base36 = id.FormatFlowflakeId(FlowflakeIdCodec.Base36);
string base58 = id.FormatFlowflakeId(FlowflakeIdCodec.Base58);
string base64Url = id.FormatFlowflakeId(FlowflakeIdCodec.Base64Url);
string crockford = id.FormatFlowflakeId(FlowflakeIdCodec.CrockfordBase32);
string hex = id.FormatFlowflakeId(FlowflakeIdCodec.Hex);

// Parse back to ID
long decoded1 = base62.ParseFlowflakeId(); // Uses Base62 by default
long decoded2 = base36.ParseFlowflakeId(FlowflakeIdCodec.Base36);
long decoded3 = base58.ParseFlowflakeId(FlowflakeIdCodec.Base58);
long decoded4 = hex.ParseFlowflakeId(FlowflakeIdCodec.Hex);

// Using Bech32 codec (requires explicit instantiation with parameters)
// Note: Bech32 is not available via enum due to required constructor parameters
var bech32Codec = new Bech32Codec("flow", bech32M: true); // HRP prefix + checksum variant
string bech32 = id.FormatFlowflakeId(bech32Codec); // e.g., "flow1..."
long decodedBech32 = bech32.ParseFlowflakeId(bech32Codec);

// Using custom codec instance
var customCodec = new MyCustomCodec();
string custom = id.FormatFlowflakeId(customCodec);
long decoded5 = custom.ParseFlowflakeId(customCodec);

// Using codec provider directly
var codec = FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Base62);
string encoded = id.FormatFlowflakeId(codec);
```

### Bech32 Codec Details

Bech32 encodes IDs in format: `{hrp}1{data}{checksum}`

**Features:**
- Limited alphabet (no ambiguous characters)
- Built-in strong checksum (6 characters)
- Suitable for manual input, printing, QR codes
- Error detection and correction capabilities

**Constructor parameters:**
- `hrp` (Human-Readable Part) - namespace prefix in lowercase (e.g., "flow", "id", "test")
- `bech32M` - checksum variant:
  - `false` - classic Bech32 (BIP-173)
  - `true` - Bech32m (BIP-350, improved error protection, recommended default)

**When to use:**
- Need resilience against typos/corruption (manual input, print, QR, messages)
- Can accept longer output (~13 data chars for 62 bits + 6 checksum + hrp + '1')

**Note:** Bech32 codec is not available via enum in `FlowflakeIdCodecProvider` due to required constructor parameters.

### Parsing Extensions

Extract information from Flowflake IDs without needing the generator instance:

```csharp
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

long id = 123456789L;

// Extract components from the ID
int instanceId = id.GetInstanceIdFromFlowflakeId();      // Extract instance ID (bits 22-30)
int sequence = id.GetSequenceNumberFromFlowflakeId();     // Extract sequence (bits 0-21)
long timestamp = id.GetTimestampFromFlowflakeId();        // Extract timestamp (seconds since epoch)

// Extract DateTime using various approaches:

// 1. Using FlowflakeClock directly
var clock = new FlowflakeClock(epoch, FlowflakeTimeSemantics.UtcNormalized);
DateTime dt1 = id.GetDateTimeFromFlowflakeId(clock);

// 2. Using FlowflakeIdOptions (from full generator config)
var options = serviceProvider.GetRequiredService<IOptions<FlowflakeIdOptions>>();
DateTime dt2 = id.GetDateTimeFromFlowflakeId(options.Value.ToFlowflakeClock());

// 3. Using FlowflakeClockOptions (decode-only config)
var clockOptions = serviceProvider.GetRequiredService<IOptions<FlowflakeClockOptions>>();
DateTime dt3 = id.GetDateTimeFromFlowflakeId(clockOptions.Value.ToFlowflakeClock());

// 4. Using explicit epoch and semantics
DateTime dt4 = id.GetDateTimeFromFlowflakeId(
    epoch: new DateTime(2023, 2, 15, 0, 0, 0, DateTimeKind.Utc),
    semantics: FlowflakeTimeSemantics.UtcNormalized);
```

## Dependencies

- `AmasiaLabs.Toolkit.FlowflakeId.Abstractions`
- `AmasiaLabs.Toolkit.FlowflakeId`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Options.DataAnnotations`