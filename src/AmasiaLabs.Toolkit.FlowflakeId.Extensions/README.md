# AmasiaLabs.Toolkit.FlowflakeId.Extensions

Dependency injection and formatting extensions for the Flowflake ID generator.

## Overview

This package provides:
- DI integration via `ServiceCollectionExtensions`
- Formatting helpers via `IdFormattingExtensions`

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
var codec = new NumericBase62Codec();
var id = 123456789L;

// Encode ID to string
string encoded = id.FormatId(codec);

// Parse string back to ID
long decoded = encoded.ParseId(codec);
```

## Dependencies

- `AmasiaLabs.Toolkit.FlowflakeId.Abstractions`
- `AmasiaLabs.Toolkit.FlowflakeId`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Options.DataAnnotations`