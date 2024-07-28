using PingLight.Core.Serializing;
using System.Text.Json.Serialization;

namespace PingLight.GatherApi.Lambda;

internal class ChangeInputModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("isLight")]
    [JsonConverter(typeof(BooleanConverter))]
    public bool IsLight { get; set; }
}
