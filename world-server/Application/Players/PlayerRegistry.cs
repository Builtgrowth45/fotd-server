using System.Collections.Concurrent;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.World.Application.Players.Registration;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.Players.Registration;
using FOMServer.World.Core.World;

namespace FOMServer.World.Application.Players
{
    internal class PlayerRegistry : IPlayerRegistry
    {
        private readonly IPlayerLoader _playerLoader;
        private readonly IPlayerRegistrationFactory _playerRegistrationFactory;
        private readonly TimeProvider _timeProvider;
        private readonly IPersistenceService _persistenceService;
        private readonly ConcurrentDictionary<uint, Player> _players = new();
        private readonly ConcurrentDictionary<NetworkAddress, Player> _playersByAddress = new();
        private readonly ConcurrentDictionary<uint, IPlayerRegistration> _playerRegistrations = new();
        private readonly ConcurrentDictionary<uint, PendingPlayer> _pendingPlayers = new();

        public PlayerRegistry(
            IPlayerLoader playerLoader,
            IPlayerRegistrationFactory playerRegistrationFactory,
            TimeProvider timeProvider,
            IPersistenceService persistenceService
        )
        {
            _playerLoader = playerLoader;
            _playerRegistrationFactory = playerRegistrationFactory;
            _timeProvider = timeProvider;
            _persistenceService = persistenceService;
        }

        public Player? Get(uint playerId)
        {
            return _players.GetValueOrDefault(playerId);
        }

        public Player? Get(NetworkAddress address)
        {
            return _playersByAddress.GetValueOrDefault(address);
        }

        public IEnumerable<Player> GetAll()
        {
            return _players.Values;
        }

        public int BroadcastToWorld<TPacket>(ref PacketWriter<TPacket> writer, uint? excludePlayerId = null)
            where TPacket : unmanaged
        {
            // Every player in this registry is already on this world server, so
            // the whole registry is the world audience. Destinations go straight
            // onto the writer rather than through an intermediate collection.
            var added = 0;
            foreach (var player in _players.Values)
            {
                if (player.Id == excludePlayerId)
                {
                    continue;
                }

                writer.AddDestination(player.Address);
                ++added;
            }

            return added;
        }

        public int BroadcastToRadius<TPacket>(
            ref PacketWriter<TPacket> writer,
            ServerPosition origin,
            ushort radius,
            uint? excludePlayerId = null
        )
            where TPacket : unmanaged
        {
            var added = 0;
            foreach (var player in _players.Values)
            {
                if (player.Id == excludePlayerId)
                {
                    continue;
                }

                if (!player.Position.IsWithinRadius(origin, radius))
                {
                    continue;
                }

                writer.AddDestination(player.Address);
                ++added;
            }

            return added;
        }

        public Player PrepareForClient(uint playerId, uint clientBinaryAddress)
        {
            var player =
                _playerLoader.Load(playerId)
                ?? throw new InvalidOperationException($"Unable to load player {playerId}");

            _pendingPlayers[playerId] = new PendingPlayer(player, clientBinaryAddress, _timeProvider.GetUtcNow());

            return player;
        }

        public Player? ClaimForClient(uint playerId, NetworkAddress sender)
        {
            if (!_pendingPlayers.TryGetValue(playerId, out var pending))
            {
                return null;
            }

            if (pending.IsExpired(_timeProvider.GetUtcNow()))
            {
                _pendingPlayers.TryRemove(new(playerId, pending));
                return null;
            }

            if (sender.BinaryAddress != pending.ExpectedBinaryAddress)
            {
                return null;
            }

            if (!_pendingPlayers.TryRemove(new(playerId, pending)))
            {
                return null;
            }

            var player = pending.Player;
            player.ClaimForClient(sender);

            var registration = _playerRegistrationFactory.Create(player);
            if (!_players.TryAdd(player.Id, player))
            {
                registration.Unregister();
                return null;
            }
            _playersByAddress[player.Address] = player;
            _playerRegistrations[player.Id] = registration;

            return player;
        }

        public void Logout(Player player)
        {
            _persistenceService.WaitForPersistence(
                player,
                () =>
                {
                    if (_playerRegistrations.TryRemove(player.Id, out var registration))
                    {
                        registration.Unregister();
                    }

                    _playersByAddress.TryRemove(new(player.Address, player));
                    _players.TryRemove(new(player.Id, player));
                }
            );
        }

        private readonly record struct PendingPlayer
        {
            private static readonly TimeSpan s_lifespan = TimeSpan.FromSeconds(30);

            private readonly DateTimeOffset _deadlineUtc;

            public PendingPlayer(Player player, uint expectedBinaryAddress, DateTimeOffset now)
            {
                Player = player;
                ExpectedBinaryAddress = expectedBinaryAddress;
                _deadlineUtc = now + s_lifespan;
            }

            public Player Player { get; }

            public uint ExpectedBinaryAddress { get; }

            public bool IsExpired(DateTimeOffset now)
            {
                return now >= _deadlineUtc;
            }
        }
    }
}
