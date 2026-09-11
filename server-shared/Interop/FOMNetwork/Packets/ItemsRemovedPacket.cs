using System.Runtime.InteropServices;
using FOMServer.Shared.Interop.FOMNetwork.Constants;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Interop.FOMNetwork.Packets
{
    [PacketId(PacketIdentifier.ID_ITEMS_REMOVED)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ItemsRemovedPacket
    {
        public uint PlayerId;
        public byte RemoveType;
        public ushort NumItemIds;
        public fixed uint RawItemIds[BufferSizes.MaxItemListSize];

        public ReadOnlySpan<uint> ItemIds
        {
            get
            {
                fixed (uint* ptr = RawItemIds)
                {
                    return new Span<uint>(ptr, NumItemIds);
                }
            }
        }
    }
}
