namespace server.Data.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class UuidV7Attribute : DefaultValueSqlAttribute {
	public UuidV7Attribute() : base("uuidv7()") {
	}
}
