using System.Collections.Concurrent;
using System.Threading.Channels;

namespace server.Services;

public sealed class ProfileEventHub {
	private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<string>>> subscribers = new();

	public ChannelReader<string> Subscribe(Guid accountId, out Guid subscriptionId) {
		var channel = Channel.CreateUnbounded<string>();
		var accountSubscribers = subscribers.GetOrAdd(accountId, _ => new ConcurrentDictionary<Guid, Channel<string>>());
		subscriptionId = Guid.NewGuid();
		accountSubscribers[subscriptionId] = channel;
		return channel.Reader;
	}

	public void Unsubscribe(Guid accountId, Guid subscriptionId) {
		if (!subscribers.TryGetValue(accountId, out var accountSubscribers)) return;
		if (accountSubscribers.TryRemove(subscriptionId, out var channel)) {
			channel.Writer.TryComplete();
		}
		if (accountSubscribers.IsEmpty) {
			subscribers.TryRemove(accountId, out _);
		}
	}

	public void Publish(Guid accountId, string payload) {
		if (!subscribers.TryGetValue(accountId, out var accountSubscribers)) return;
		foreach (var channel in accountSubscribers.Values) {
			channel.Writer.TryWrite(payload);
		}
	}
}

