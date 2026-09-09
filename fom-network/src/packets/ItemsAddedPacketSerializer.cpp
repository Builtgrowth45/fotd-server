#include <fom-network/packets/ItemsAddedPacket.h>

#include "../structs/item/ItemListInteropSerializer.h"
#include "PacketSerializers.h"

namespace FOMNetwork {

void ItemsAddedPacketSerializer::Write(RakNet::BitStream& bs,
                                       const ItemsAddedPacket* data) const {
  ItemListInteropSerializer itemListSerializer;

  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(data->to);
  bs.WriteCompressed(data->toSlot);
  itemListSerializer.Write(bs, data->items);
}

}  // namespace FOMNetwork
