namespace server.Data.Entities;

public interface IAuditable {
    DateTime UpdatedAtUtc { get; set; }

    DateTime CreatedAtUtc { get; set; }
}