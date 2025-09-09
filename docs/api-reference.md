# API Reference

Welcome to the K.EntityFrameworkCore API Reference documentation.

## Overview

This section contains the complete API documentation for K.EntityFrameworkCore, automatically generated from the source code. The API is organized into several key namespaces:

## Core Namespaces

### K.EntityFrameworkCore
Contains the core types and classes:
- `Topic<T>` - Typed Kafka topic representation
- `InboxMessage` - Message for inbox pattern implementation
- `OutboxMessage` - Message for outbox pattern implementation  
- `Envelope<T>` - Message wrapper with headers and metadata

### K.EntityFrameworkCore.Extensions
Extension methods and builder patterns:
- `DbContextExtensions` - Entity Framework DbContext extensions
- `ServiceCollectionExtensions` - Dependency injection extensions
- `ModelBuilderExtensions` - EF model configuration extensions
- Various builder classes for fluent configuration

### K.EntityFrameworkCore.Interfaces
Interface definitions for extensibility:
- `IProducer<T>` - Message producer interface
- `IConsumer<T>` - Message consumer interface
- `IMessageSerializer<T>` - Message serialization interface
- `IMiddleware<T>` - Middleware pipeline interface

### K.EntityFrameworkCore.Middlewares
Middleware implementations:
- **Inbox** - Message deduplication middleware
- **Outbox** - Reliable message publishing middleware  
- **Forget** - Fire-and-forget messaging middleware

## Getting Started

For getting started guides and examples, see:
- [Installation Guide](getting-started/installation.md)
- [Quick Start](getting-started/quick-start.md)
- [Basic Usage Examples](getting-started/basic-usage.md)

## Browse API Documentation

The complete API reference with detailed method signatures, parameters, and examples is generated automatically from the source code.
