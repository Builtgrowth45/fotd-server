using System.Collections.Concurrent;
using FOMServer.Master.Core.Players;
using FOMServer.Shared.Core.Persistence;

namespace FOMServer.Master.Application.Players
{
    internal class PlayerRegistry : IPlayerRegistry
    {
        private readonly IPersistenceService _persistenceService;
        private readonly ConcurrentDictionary<uint, Player> _players = new();

        public PlayerRegistry(IPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        public Player? Get(uint playerId)
        {
            return _players.GetValueOrDefault(playerId);
        }

        public Player Login(ClientSession session)
        {
            if (!session.PlayerId.HasValue)
            {
                throw new InvalidOperationException("Session login must be started before it can be completed");
            }

            var playerId = session.PlayerId.Value;

            var player = new Player(playerId, session);

            if (!_players.TryAdd(playerId, player))
            {
                throw new InvalidOperationException($"Inventory {playerId} is already logged in");
            }

            session.CompleteLogin(player);
            _persistenceService.Register(player);

            // The client may have left while the player was being built. Nothing
            // will come back for it, so release it here rather than leaving it
            // stranded in the registry with no session behind it.
            if (session.IsDisconnected)
            {
                Logout(player);
            }

            return player;
        }

        public void Logout(Player player)
        {
            _persistenceService.WaitForPersistence(player, () => _players.TryRemove(new(player.Id, player)));
        }

        public void LogoutSession(ClientSession session)
        {
            // A login that had not reached CompleteLogin leaves the session without
            // a player, so fall back to the id the login started with.
            var player = session.Player;
            if (player is null && session.PlayerId.HasValue)
            {
                player = Get(session.PlayerId.Value);
            }

            if (player is null)
            {
                return;
            }

            Logout(player);
        }
    }
}
