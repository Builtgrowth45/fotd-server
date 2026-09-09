using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.World.Core.World;

namespace FOMServer.World.Core.Players
{
    internal interface IPlayerRegistry
    {
        Player? Get(uint playerId);

        Player? Get(NetworkAddress address);

        IEnumerable<Player> GetAll();

        /// <summary>
        /// Adds every player on this world server as a destination on the packet.
        /// </summary>
        /// <param name="writer">The packet to add destinations to.</param>
        /// <param name="excludePlayerId">A player to leave out, usually the one that caused the packet.</param>
        /// <returns>The number of destinations added.</returns>
        int BroadcastToWorld<TPacket>(ref PacketWriter<TPacket> writer, uint? excludePlayerId = null)
            where TPacket : unmanaged;

        /// <summary>
        /// Adds every player within <paramref name="radius"/> of a position as a destination on the packet.
        /// </summary>
        /// <param name="writer">The packet to add destinations to.</param>
        /// <param name="origin">The position to measure from.</param>
        /// <param name="radius">How far out to reach.</param>
        /// <param name="excludePlayerId">A player to leave out, usually the one that caused the packet.</param>
        /// <returns>The number of destinations added.</returns>
        int BroadcastToRadius<TPacket>(
            ref PacketWriter<TPacket> writer,
            ServerPosition origin,
            ushort radius,
            uint? excludePlayerId = null
        )
            where TPacket : unmanaged;

        Player PrepareForClient(uint playerId, uint clientBinaryAddress);

        Player? ClaimForClient(uint playerId, NetworkAddress sender);

        void Logout(Player player);
    }
}
