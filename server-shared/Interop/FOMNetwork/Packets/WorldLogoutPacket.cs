using System.Runtime.InteropServices;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Interop.FOMNetwork.Packets
{
    [PacketId(PacketIdentifier.ID_WORLD_LOGOUT)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldLogoutPacket
    {
        public uint PlayerId;

        /// <summary>
        /// Set when the player is moving to another world rather than leaving
        /// the game, so the session survives the handover.
        /// </summary>
        public byte IsChangingWorlds;
    }
}
