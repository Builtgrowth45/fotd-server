#include <fom-network/packets/WorldLogoutPacket.h>

#include "PacketSerializers.h"

namespace FOMNetwork {

bool WorldLogoutPacketSerializer::Read(RakNet::BitStream& bs,
                                       WorldLogoutPacket* data) const {
  if (!bs.ReadCompressed(data->playerId)) return false;
  if (!bs.Read(data->isChangingWorlds)) return false;

  return true;
}

void WorldLogoutPacketSerializer::Write(RakNet::BitStream& bs,
                                        const WorldLogoutPacket* data) const {
  bs.WriteCompressed(data->playerId);
  bs.Write(data->isChangingWorlds);
}

}  // namespace FOMNetwork
