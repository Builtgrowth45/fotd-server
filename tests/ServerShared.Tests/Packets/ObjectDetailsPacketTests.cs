using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Utilities;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.Shared.Interop.FOMNetwork.Structs.Faction;

namespace FOMServer.Shared.Tests.Packets
{
    public class ObjectDetailsPacketTests
    {
        [Fact]
        public unsafe void Layout_MatchesTheNativeStruct()
        {
            var packet = new ObjectDetailsPacket();
            var start = (byte*)&packet;
            var emblemSize = sizeof(FactionEmblemInterop);

            Assert.Equal(0, Offset(start, (byte*)&packet.ObjectId));
            Assert.Equal(4, Offset(start, &packet.Type));
            Assert.Equal(5, Offset(start, &packet.HasDetails));
            Assert.Equal(6, Offset(start, (byte*)&packet.PlayerId));
            Assert.Equal(10, Offset(start, packet.RawName));
            Assert.Equal(30, Offset(start, packet.RawUnknownString32));
            Assert.Equal(62, Offset(start, packet.RawUnknownString64));
            Assert.Equal(126, Offset(start, (byte*)&packet.Emblem));
            Assert.Equal(126 + emblemSize, Offset(start, &packet.UnknownByte));
            Assert.Equal(127 + emblemSize, Offset(start, packet.RawUnknownTag1));
            Assert.Equal(131 + emblemSize, Offset(start, &packet.UnknownTag1Value));
            Assert.Equal(132 + emblemSize, Offset(start, packet.RawUnknownTag2));
            Assert.Equal(136 + emblemSize, Offset(start, &packet.UnknownTag2Value));
            Assert.Equal(137 + emblemSize, Offset(start, &packet.UnknownFlag1));
            Assert.Equal(138 + emblemSize, Offset(start, &packet.UnknownFlag2));
            Assert.Equal(139 + emblemSize, sizeof(ObjectDetailsPacket));
        }

        [Fact]
        public void Identifier_AndSize_AreRegistered()
        {
            Assert.Equal(PacketIdentifier.ID_OBJECT_DETAILS, PacketHelpers.GetPacketTypeId<ObjectDetailsPacket>());
            Assert.Equal(Marshal.SizeOf<ObjectDetailsPacket>(), PacketHelpers.GetPacketSize<ObjectDetailsPacket>());
        }

        [Fact]
        public void Name_KeepsRoomForTheTerminator()
        {
            var packet = new ObjectDetailsPacket { Name = new string('a', 25) };

            Assert.Equal(new string('a', 19), packet.Name);
        }

        private static unsafe int Offset(byte* start, byte* field)
        {
            return (int)(field - start);
        }
    }
}
