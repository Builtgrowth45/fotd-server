using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Utilities;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Enums.Item;
using FOMServer.Shared.Interop.FOMNetwork.Packets;

namespace FOMServer.Shared.Tests.Packets
{
    public class ItemsAddedPacketTests
    {
        [Fact]
        public void Layout_MatchesTheDisassembledStructure()
        {
            // Offsets from Packet_ID_ITEMS_ADDED, relative to the end of the
            // variable sized base that the native struct does not carry.
            Assert.Equal(0, Marshal.OffsetOf<ItemsAddedPacket>(nameof(ItemsAddedPacket.PlayerId)).ToInt32());
            Assert.Equal(4, Marshal.OffsetOf<ItemsAddedPacket>(nameof(ItemsAddedPacket.To)).ToInt32());
            Assert.Equal(5, Marshal.OffsetOf<ItemsAddedPacket>(nameof(ItemsAddedPacket.ToSlot)).ToInt32());
            Assert.Equal(6, Marshal.OffsetOf<ItemsAddedPacket>(nameof(ItemsAddedPacket.Items)).ToInt32());
        }

        [Fact]
        public void Identifier_AndSize_AreRegistered()
        {
            Assert.Equal(PacketIdentifier.ID_ITEMS_ADDED, PacketHelpers.GetPacketTypeId<ItemsAddedPacket>());
            Assert.Equal(Marshal.SizeOf<ItemsAddedPacket>(), PacketHelpers.GetPacketSize<ItemsAddedPacket>());
        }

        [Fact]
        public void Fields_RoundTripThroughAPooledWriter()
        {
            // The item list makes this packet a couple of megabytes, far too large
            // for a stack local, so it is written through the pooled buffer that
            // the send path uses.
            var address = new NetworkAddress { BinaryAddress = 0x0100007F, Port = 7777 };
            using var writer = new PacketWriter<ItemsAddedPacket>(address);

            ref var data = ref writer.Data;
            data.PlayerId = 7;
            data.To = ItemContainerType.Inventory;
            data.ToSlot = ItemSlotType.None;
            data.Items.ItemCount = 2;
            data.Items.MaxSpace = 100;
            data.Items.Items[0].Id = 11;
            data.Items.Items[1].Id = 22;

            Assert.Equal(7u, writer.Data.PlayerId);
            Assert.Equal(ItemContainerType.Inventory, writer.Data.To);
            Assert.Equal(2u, writer.Data.Items.ItemCount);
            Assert.Equal(11u, writer.Data.Items.Items[0].Id);
            Assert.Equal(22u, writer.Data.Items.Items[1].Id);

            var packet = writer.Build();
            packet.Release();
        }
    }
}
