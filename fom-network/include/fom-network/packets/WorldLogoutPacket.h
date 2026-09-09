#pragma once

#include <fom-network/Interop.h>

namespace FOMNetwork {

#pragma pack(push, 1)
struct WorldLogoutPacket {
  uint32_t playerId;
  // Set when the player is moving to another world rather than leaving the
  // game, so the world server can keep the session alive for the handover.
  uint8_t isChangingWorlds;
};
#pragma pack(pop)

ASSERT_BLITTABLE(WorldLogoutPacket);

}  // namespace FOMNetwork
