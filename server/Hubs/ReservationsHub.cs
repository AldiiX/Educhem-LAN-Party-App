using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto.Mappers;
using server.Infrastructure;
using server.Services;

namespace server.Hubs;

public sealed class ReservationsHub(
	IAuthService auth,
	AppDbContext db
) : Hub {
	public static readonly HashSet<string> ConnectedIds = [];



	public override async Task OnConnectedAsync() {
		ConnectedIds.Add(Context.ConnectionId);
		await Clients.Caller.SendAsync("ReceiveReservations", JsonSerializer.Serialize(new { Reservations = await FetchReservations() }, JsonSerializerOptions.Web));
		await Clients.All.SendAsync("ReceiveStatus", JsonSerializer.Serialize(new { connectedIds =  ConnectedIds.Count }, JsonSerializerOptions.Web));
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		ConnectedIds.Remove(Context.ConnectionId);
		await Clients.All.SendAsync("ReceiveStatus", JsonSerializer.Serialize(new { connectedIds =  ConnectedIds.Count }, JsonSerializerOptions.Web));
		await base.OnDisconnectedAsync(exception);
	}



	// helpery metodiky
	public async Task<List<object>> FetchReservations() {
		var account = await auth.ReAuthAsync();
		var r = db.ReservationsEf().AsNoTracking().ToList();

		return account == null
			? r.Select(x => x.ToAnonymousDto()).Cast<object>().ToList()
			: r.Select(x => x.ToDto()).Cast<object>().ToList();
	}
}