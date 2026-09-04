namespace server.Infrastructure.HubRateLimiting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class HubRateLimitAttribute : Attribute {
	public string PolicyName { get; }
	public string? CustomErrorMessage { get; init; }

	public HubRateLimitAttribute(string policyName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
		PolicyName = policyName;
	}
}
