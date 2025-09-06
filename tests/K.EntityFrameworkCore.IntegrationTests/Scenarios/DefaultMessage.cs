using System.Text.Json.Serialization;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public record DefaultMessage(int Id, string Name);

public record AlternativeMessage(int Id, string Name);

public record ExtendedMessage(int Id, string Name, string Category, decimal Value) : DefaultMessage(Id, Name);
