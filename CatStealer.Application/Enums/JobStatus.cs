using System.Text.Json.Serialization;

namespace CatStealer.Application.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum JobStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    NotFound
}