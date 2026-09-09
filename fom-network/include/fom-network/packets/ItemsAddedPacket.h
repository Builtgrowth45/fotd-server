#pragma once

#include <fom-network/Interop.h>
#include <fom-network/enums/item/ItemContainerType.h>
#include <fom-network/enums/item/ItemSlotType.h>
#include <fom-network/structs/item/ItemListInterop.h>

namespace FOMNetwork {

#pragma pack(push, 1)
struct ItemsAddedPacket {
  uint32_t playerId;
  Enum::ItemContainerType to;
  Enum::ItemSlotType toSlot;
  ItemListInterop items;
};
#pragma pack(pop)

ASSERT_BLITTABLE(ItemsAddedPacket);

}  // namespace FOMNetwork
