#pragma once

#include <fom-network/Interop.h>
#include <fom-network/constants/BufferSizes.h>
#include <fom-network/structs/faction/FactionEmblemInterop.h>

namespace FOMNetwork {

#pragma pack(push, 1)
struct ObjectDetailsPacket {
  uint32_t objectId;
  uint8_t type;
  uint8_t hasDetails;

  // Only sent when hasDetails is not set.
  uint32_t playerId;

  // Only sent when hasDetails is set.
  uint8_t name[BufferSizes::PLAYER_NAME];
  uint8_t unknownString32[32];
  uint8_t unknownString64[64];
  FactionEmblemInterop emblem;
  uint8_t unknownByte;
  uint8_t unknownTag1[4];
  uint8_t unknownTag1Value;
  uint8_t unknownTag2[4];
  uint8_t unknownTag2Value;
  uint8_t unknownFlag1;
  uint8_t unknownFlag2;
};
#pragma pack(pop)

ASSERT_BLITTABLE(ObjectDetailsPacket);

}  // namespace FOMNetwork
