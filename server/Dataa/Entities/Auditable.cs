namespace server.Data.Entities;

public abstract class Auditable : IAuditable {
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}