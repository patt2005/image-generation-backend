using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoAiBackend.Persistance.Entities;

[Table("enhance-jobs")]
public class EnhanceJob
{
    [Column("id")]
    public string Id { get; set; }
    [Column("status")]
    public EnhanceStatus Status { get; set; }
    [Column("created-at")]
    public DateTime CreatedAt { get; set; }
    [Column("output")]
    public string Output { get; set; }
    [Column("user-id")]
    public Guid UserId { get; set; }
    public User? User { get; set; }
}