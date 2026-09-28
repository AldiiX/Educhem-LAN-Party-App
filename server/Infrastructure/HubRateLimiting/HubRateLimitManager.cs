using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace server.Infrastructure.HubRateLimiting;

public sealed class HubRateLimitManager : IDisposable {
	public const string ReservationsMutationPolicy = "reservations-mutation";

	private readonly ConcurrentDictionary<string, PartitionedRateLimiter<string>> limiters = new();

	public HubRateLimitManager(IConfiguration? configuration = null) {
		var permitLimit = configuration?.GetValue<int?>("RateLimiting:SignalR:ReservationsMutation:PermitLimit") ?? 5;
		var windowSeconds = configuration?.GetValue<int?>("RateLimiting:SignalR:ReservationsMutation:WindowSeconds") ?? 10;
		var segments = configuration?.GetValue<int?>("RateLimiting:SignalR:ReservationsMutation:SegmentsPerWindow") ?? 2;

		RegisterSlidingWindowPolicy(
			ReservationsMutationPolicy,
			permitLimit,
			TimeSpan.FromSeconds(windowSeconds),
			segments
		);
	}

	public void RegisterSlidingWindowPolicy(string policyName, int permitLimit, TimeSpan window, int segmentsPerWindow = 2) {
		ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

		var limiter = PartitionedRateLimiter.Create<string, string>(key => RateLimitPartition.GetSlidingWindowLimiter(
			key,
			_ => new SlidingWindowRateLimiterOptions {
				PermitLimit = permitLimit,
				Window = window,
				SegmentsPerWindow = Math.Max(1, segmentsPerWindow),
				QueueLimit = 0,
			}
		));

		limiters.AddOrUpdate(policyName, limiter, (_, oldLimiter) => {
			oldLimiter.Dispose();
			return limiter;
		});
	}

	public RateLimitLease AttemptAcquire(string policyName, string key) {
		if (limiters.TryGetValue(policyName, out var limiter)) {
			return limiter.AttemptAcquire(key);
		}

		return NullRateLimitLease.Instance;
	}

	public void Dispose() {
		foreach (var limiter in limiters.Values) {
			limiter.Dispose();
		}
		limiters.Clear();
	}

	private sealed class NullRateLimitLease : RateLimitLease {
		public static readonly NullRateLimitLease Instance = new();
		public override bool IsAcquired => true;
		public override IEnumerable<string> MetadataNames => [];
		public override bool TryGetMetadata(string metadataName, out object? metadata) {
			metadata = null;
			return false;
		}
	}
}
