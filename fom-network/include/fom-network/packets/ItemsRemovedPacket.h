#pragma once

#include <fom-network/Interop.h>
#include <fom-network/constants/BufferSizes.h>

namespace FOMNetwork {

#pragma pack(push, 1)
struct ItemsRemovedPacket {
  uint32_t playerId;
  uint8_t removeType;
  uint16_t numItemIds;
  uint32_t itemIds[BufferSizes::MAX_ITEM_LIST_SIZE];
};
#pragma pack(pop)

ASSERT_BLITTABLE(ItemsRemovedPacket);

}  // namespace FOMNetwork
