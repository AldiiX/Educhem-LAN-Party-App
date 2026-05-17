namespace server.Data.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultValueSqlAttribute(string sql) : Attribute {
	public string Sql { get; } = sql;
}
