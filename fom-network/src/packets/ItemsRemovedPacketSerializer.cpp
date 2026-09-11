#include <fom-network/packets/ItemsRemovedPacket.h>

#include "PacketSerializers.h"

namespace FOMNetwork {

void ItemsRemovedPacketSerializer::Write(RakNet::BitStream& bs,
                                         const ItemsRemovedPacket* data) const {
  auto numItemIds = data->numItemIds;
  if (numItemIds > BufferSizes::MAX_ITEM_LIST_SIZE)
    numItemIds = BufferSizes::MAX_ITEM_LIST_SIZE;

  bs.WriteCompressed(data->playerId);
  bs.WriteCompressed(data->removeType);

  bs.WriteCompressed(numItemIds);
  for (int i = 0; i < numItemIds; ++i) {
    bs.WriteCompressed(data->itemIds[i]);
  }
}

}  // namespace FOMNetwork
