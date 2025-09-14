# AmasiaLabs.Toolkit.FlowflakeId.Abstractions

Abstractions for the Flowflake ID generator - a Snowflake-like distributed unique ID generator with configurable epoch and time source.

## Overview

This package contains the core abstractions and interfaces for the Flowflake ID system:
- `IFlowflakeId` - Main interface for ID generation
- `IIdCodec` - Interface for encoding/decoding IDs
- `FlowflakeIdOptions` - Configuration options
- `FlowflakeLayout` - ID bit layout configuration
- `FlowflakeTimeSemantics` - Time interpretation modes

## Installation

```bash
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Abstractions
```

## Usage

Reference this package when you need to work with Flowflake ID interfaces without depending on the concrete implementation.

For the full implementation, install `AmasiaLabs.Toolkit.FlowflakeId`.
For DI integration and extensions, install `AmasiaLabs.Toolkit.FlowflakeId.Extensions`.