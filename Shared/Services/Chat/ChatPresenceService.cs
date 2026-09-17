using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Shared.Interfaces.Chat;


namespace Shared.Services.Chat
{
    public class ChatPresenceService : IChatPresenceService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connections = new();

        public Task AddConnectionAsync(
            string userId,
            string connectionId)
        {
            var connections = _connections.GetOrAdd(
                userId,
                _ => new ConcurrentDictionary<string, byte>());

            connections.TryAdd(connectionId, 0);

            return Task.CompletedTask;
        }

        public Task<bool> RemoveConnectionAsync(
            string userId,
            string connectionId)
        {
            if (!_connections.TryGetValue(
                    userId,
                    out var connections))
            {
                return Task.FromResult(true);
            }

            connections.TryRemove(
                connectionId,
                out _);

            if (!connections.IsEmpty)
                return Task.FromResult(false);

            _connections.TryRemove(
                userId,
                out _);

            return Task.FromResult(true);
        }

        public bool IsOnline(string userId)
        {
            return _connections.TryGetValue(
                       userId,
                       out var connections)
                   && !connections.IsEmpty;
        }

        public IReadOnlyCollection<string> GetOnlineAdminIds()
        {
            return _connections
                .Where(x => !x.Value.IsEmpty)
                .Select(x => x.Key)
                .ToList();
        }
        public bool IsAnyOnline()
        {
            return _connections.Any(x => !x.Value.IsEmpty);
        }
    }
}
