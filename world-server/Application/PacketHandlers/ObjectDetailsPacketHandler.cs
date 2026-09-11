using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.PacketHandlers;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.PacketHandlers
{
    [PacketHandler]
    internal class ObjectDetailsPacketHandler : PacketHandlerBase<ObjectDetailsPacket>
    {
        /// <summary>
        /// The only type the client requests, and the only one it applies when details arrive.
        /// </summary>
        private const byte CharacterType = 1;

        /// <summary>
        /// The value the client gives this field before any details have arrived.
        /// </summary>
        private const byte UnknownByteDefault = 3;

        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<ObjectDetailsPacketHandler> _logger;

        public ObjectDetailsPacketHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<ObjectDetailsPacketHandler> logger
        )
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in ObjectDetailsPacket p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received unexpected packet for player {PlayerId}", p.PlayerId);
                return;
            }

            // Details only ever flow from the server to the client.
            if (p.HasDetails != 0 || p.Type != CharacterType)
            {
                _logger.LogDebug(
                    "Ignoring object details from player {PlayerId} (type {Type}, has details {HasDetails})",
                    player.Id,
                    p.Type,
                    p.HasDetails
                );
                return;
            }

            // Clients know characters by their player ID.
            var target = _playerRegistry.Get(p.ObjectId);
            if (target is null)
            {
                return;
            }

            using var response = new PacketWriter<ObjectDetailsPacket>(sender);
            ref var rData = ref response.Data;
            rData.ObjectId = target.Id;
            rData.Type = CharacterType;
            rData.HasDetails = 1;
            rData.Name = target.Name;
            rData.UnknownByte = UnknownByteDefault;
            _clientPacketSender.Send(response.Build());
        }
    }
}
