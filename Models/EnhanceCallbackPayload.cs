using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PhotoAiBackend.Models
{
    public class EnhanceCallbackPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("input")]
        public PredictionInput Input { get; set; }

        [JsonPropertyName("output")]
        public List<string>? Output { get; set; }

        [JsonPropertyName("logs")]
        public string? Logs { get; set; }

        [JsonPropertyName("data_removed")]
        public bool DataRemoved { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("urls")]
        public PredictionUrls Urls { get; set; }
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

    public class PredictionUrls
    {
        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("get")]
        public string Get { get; set; }

        [JsonPropertyName("stream")]
        public string Stream { get; set; }
    }
}