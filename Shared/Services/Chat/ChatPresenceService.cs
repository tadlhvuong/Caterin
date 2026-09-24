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
        public Task<bool> AddConnectionAsync(
    string userId,
    string connectionId)
        {
            var connections = _connections.GetOrAdd(
                userId,
                _ => new ConcurrentDictionary<string, byte>());

            var wasOffline = connections.IsEmpty;

            connections.TryAdd(connectionId, 0);

            return Task.FromResult(wasOffline);
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

        public Task<bool> JoinConversationAsync(
    string userId,
    string connectionId,
    long conversationId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(connectionId))
                return Task.FromResult(false);

            if (conversationId <= 0)
                return Task.FromResult(false);

            var connections =
                _connections.GetOrAdd(conversationId.ToString(),  _ => new ConcurrentDictionary<string, byte>());

            var added = connections.TryAdd(
                connectionId,
                0);

            return Task.FromResult(added);
        }
        public Task<bool> LeaveConversationAsync(
    string userId,
    string connectionId,
    long conversationId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(connectionId))
                return Task.FromResult(false);

            if (conversationId <= 0)
                return Task.FromResult(false);

            var key = conversationId.ToString();

            if (!_connections.TryGetValue(
                key,
                out var connections))
            {
                return Task.FromResult(false);
            }

            var removed = connections.TryRemove(
                connectionId,
                out _);

            /*
             * Không còn connection nào trong conversation
             * thì xóa luôn conversation khỏi dictionary.
             */
            if (connections.IsEmpty)
            {
                _connections.TryRemove(
                    key,
                    out _);
            }

            return Task.FromResult(removed);
        }
        public bool IsConversationActive(
    long conversationId)
        {
            var key = conversationId.ToString();

            return
                _connections.TryGetValue(
                    key,
                    out var connections)
                && !connections.IsEmpty;
        }

        public Task RemoveConnectionFromConversationsAsync(
    string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                return Task.CompletedTask;

            foreach (var pair in _connections)
            {
                var conversationId = pair.Key;
                var connections = pair.Value;

                connections.TryRemove(
                    connectionId,
                    out _);

                /*
                 * Không còn connection nào đang mở
                 * conversation này thì xóa conversation khỏi dictionary.
                 */
                if (connections.IsEmpty)
                {
                    _connections.TryRemove(
                        conversationId,
                        out _);
                }
            }

            return Task.CompletedTask;
        }
    }
}
