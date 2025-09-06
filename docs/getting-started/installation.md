# Installation

This guide covers how to install and set up K-Entity-Framework in your .NET project.

## Package Installation

### Via Package Manager Console

```powershell
Install-Package K.EntityFrameworkCore
```

### Via .NET CLI

```bash
dotnet add package K.EntityFrameworkCore
```

### Via PackageReference

Add the following to your `.csproj` file:

```xml
<PackageReference Include="K.EntityFrameworkCore" Version="latest" />
```

## Dependencies

K-Entity-Framework requires the following dependencies (automatically installed):

- **Microsoft.EntityFrameworkCore** (8.0+)
- **Confluent.Kafka** (2.10.1)

## Optional Dependencies

Depending on your needs, you may want to install additional packages:

### For SQL Server Integration
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### For PostgreSQL Integration
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```
## Framework Compatibility

| Framework | Supported |
|-----------|-----------|
| .NET 8.0+ | ✅         |
| .NET 9.0+ | ✅         |

## Next Steps

- [Quick Start Guide](quick-start.md) - Build your first producer and consumer
- [Basic Usage](basic-usage.md) - Learn the fundamentals