# AmasiaLabs.Toolkit.FlowflakeId.Extensions

Dependency injection and formatting extensions for the Flowflake ID generator.

## Overview

This package provides:
- DI integration via `ServiceCollectionExtensions`
- Formatting helpers via `FlowflakeIdFormattingExtensions`
- Parsing helpers via `FlowflakeIdParsingExtensions`
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

```csharp
// Using configuration
services.AddFlowflakeId(configuration);

// Using configuration section
services.AddFlowflakeId(configuration.GetSection("FlowflakeId"));

// Using code-based configuration
services.AddFlowflakeId(options =>
{
    options.Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    options.InstanceId = 1;
});
```

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

long id = 123456789L;

// Extract components from the ID
int instanceId = id.GetInstanceIdFromFlowflakeId();      // Extract instance ID (bits 22-30)
int sequence = id.GetSequenceNumberFromFlowflakeId();     // Extract sequence (bits 0-21)
long timestamp = id.GetTimestampFromFlowflakeId();        // Extract timestamp (seconds since epoch)

// Note: To get DateTime, use the generator's GetDateTime(id) method
// as it requires knowledge of the epoch and time semantics
```

## Dependencies

- `AmasiaLabs.Toolkit.FlowflakeId.Abstractions`
- `AmasiaLabs.Toolkit.FlowflakeId`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Options.DataAnnotations`