using System.Text.Json.Serialization;
using CatStealer.Application.Enums;

namespace CatStealer.Application.Common;

public class JobInfo
{
    public required string Id { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JobStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? FinishedAt { get; set; }
    
    public object? Result { get; set; }
    
    public string? ErrorMessage { get; set; }
}