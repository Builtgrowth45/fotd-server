using FOMServer.Shared.Core.PacketHandlers;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.PacketHandlers
{
    [PacketHandler]
    internal class WorldLogoutPacketHandler : PacketHandlerBase<WorldLogoutPacket>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ILogger<WorldLogoutPacketHandler> _logger;

        public WorldLogoutPacketHandler(IPlayerRegistry playerRegistry, ILogger<WorldLogoutPacketHandler> logger)
        {
            _playerRegistry = playerRegistry;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in WorldLogoutPacket p)
        {
            // The sender's own registration decides who leaves. The packet carries
            // a player id, but acting on it would let a client log out somebody
            // else.
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received world logout from unregistered client '{Sender}'", sender);
                return;
            }

            if (p.PlayerId != player.Id)
            {
                _logger.LogWarning("Player {PlayerId} tried to log out player {ClaimedId}", player.Id, p.PlayerId);
            }

            // A world change keeps the player alive on the master server, which
            // hands them to the next world. Leaving the game does not.
            if (p.IsChangingWorlds != 0)
            {
                _logger.LogInformation("Player {PlayerId} is leaving for another world", player.Id);
            }

            _playerRegistry.Logout(player);
        }
    }
}
