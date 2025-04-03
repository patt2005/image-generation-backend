using System.Text.Json.Serialization;

namespace PhotoAiBackend.Models;

public class EnhanceCallbackPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime CompletedAt { get; set; }

    [JsonPropertyName("input")]
    public PredictionInput Input { get; set; }

    [JsonPropertyName("output")]
    public List<string> Output { get; set; }

    [JsonPropertyName("metrics")]
    public PredictionMetrics Metrics { get; set; }
}

public class PredictionInput
{
    [JsonPropertyName("input")]
    public string InputImage { get; set; }

    [JsonPropertyName("background_upsampler")]
    public string BackgroundUpsampler { get; set; }

    [JsonPropertyName("upscaling_model_type")]
    public string UpscalingModelType { get; set; }

    [JsonPropertyName("super_resolution_factor")]
    public int SuperResolutionFactor { get; set; }
}

public class PredictionMetrics
{
    [JsonPropertyName("predict_time")]
    public double PredictTime { get; set; }
}