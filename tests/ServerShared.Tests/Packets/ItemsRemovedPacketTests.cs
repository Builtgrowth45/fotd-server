using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Utilities;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Packets;

namespace FOMServer.Shared.Tests.Packets
{
    public class ItemsRemovedPacketTests
    {
        [Fact]
        public void Layout_MatchesTheNativeStruct()
        {
            Assert.Equal(0, Marshal.OffsetOf<ItemsRemovedPacket>(nameof(ItemsRemovedPacket.PlayerId)).ToInt32());
            Assert.Equal(4, Marshal.OffsetOf<ItemsRemovedPacket>(nameof(ItemsRemovedPacket.RemoveType)).ToInt32());
            Assert.Equal(5, Marshal.OffsetOf<ItemsRemovedPacket>(nameof(ItemsRemovedPacket.NumItemIds)).ToInt32());
            Assert.Equal(7, Marshal.OffsetOf<ItemsRemovedPacket>(nameof(ItemsRemovedPacket.RawItemIds)).ToInt32());
        }

        [Fact]
        public void Identifier_AndSize_AreRegistered()
        {
            Assert.Equal(PacketIdentifier.ID_ITEMS_REMOVED, PacketHelpers.GetPacketTypeId<ItemsRemovedPacket>());
            Assert.Equal(Marshal.SizeOf<ItemsRemovedPacket>(), PacketHelpers.GetPacketSize<ItemsRemovedPacket>());
        }
    }
}
