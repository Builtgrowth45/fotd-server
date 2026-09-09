using System.Runtime.InteropServices;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Enums.Item;
using FOMServer.Shared.Interop.FOMNetwork.Structs.Item;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Interop.FOMNetwork.Packets
{
    [PacketId(PacketIdentifier.ID_ITEMS_ADDED)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ItemsAddedPacket
    {
        public uint PlayerId;
        public ItemContainerType To;
        public ItemSlotType ToSlot;
        public ItemListInterop Items;
    }
}
