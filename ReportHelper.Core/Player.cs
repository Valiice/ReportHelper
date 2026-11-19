using System.Text.Json.Serialization;

namespace ReportHelper.Core;

public sealed record Player(string Name, string World)
{
    [JsonIgnore]
    public string Display => $"{this.Name}@{this.World}";
}
