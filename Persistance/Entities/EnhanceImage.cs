using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoAiBackend.Persistance.Entities;

[Table("enhance-images")]
public class EnhanceImage
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("job-id")]
    public string JobId { get; set; }
    public EnhanceJob Job { get; set; }
    [Column("data")]
    public byte[] Data { get; set; }
    [Column("mime-type")]
    public string MimeType { get; set; } = "image/png";
}