using System.Runtime.InteropServices;
using FOMServer.Shared.Interop.FOMNetwork.Constants;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Structs.Faction;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Interop.FOMNetwork.Packets
{
    [PacketId(PacketIdentifier.ID_OBJECT_DETAILS)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ObjectDetailsPacket
    {
        public uint ObjectId;
        public byte Type;
        public byte HasDetails;
        public uint PlayerId; // HasDetails == 0
        public fixed byte RawName[BufferSizes.PlayerName]; // HasDetails != 0 (through UnknownFlag2)
        public fixed byte RawUnknownString32[32];
        public fixed byte RawUnknownString64[64];
        public FactionEmblemInterop Emblem;
        public byte UnknownByte;
        public fixed byte RawUnknownTag1[4];
        public byte UnknownTag1Value;
        public fixed byte RawUnknownTag2[4];
        public byte UnknownTag2Value;
        public byte UnknownFlag1;
        public byte UnknownFlag2;

        public string Name
        {
            get
            {
                fixed (byte* ptr = RawName)
                {
                    return CStringParser.ToString(ptr, BufferSizes.PlayerName);
                }
            }
            set
            {
                fixed (byte* ptr = RawName)
                {
                    CStringParser.FromString(value, ptr, BufferSizes.PlayerName);
                }
            }
        }
    }
}
