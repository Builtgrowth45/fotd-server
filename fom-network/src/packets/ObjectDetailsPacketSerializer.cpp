#include <fom-network/packets/ObjectDetailsPacket.h>

#include "../structs/faction/FactionEmblemInteropSerializer.h"
#include "PacketSerializers.h"

namespace FOMNetwork {

bool ObjectDetailsPacketSerializer::Read(RakNet::BitStream& bs,
                                         ObjectDetailsPacket* data) const {
  FactionEmblemInteropSerializer emblemSerializer;
  bool bit;

  if (!bs.ReadCompressed(data->objectId)) return false;
  if (!bs.ReadCompressed(data->type)) return false;
  if (!bs.Read(bit)) return false;
  data->hasDetails = bit;

  if (!data->hasDetails) {
    if (!bs.ReadCompressed(data->playerId)) return false;
    return true;
  }

  if (!DecodeString(bs, data->name)) return false;
  if (!DecodeString(bs, data->unknownString32)) return false;
  if (!DecodeString(bs, data->unknownString64)) return false;
  if (!emblemSerializer.Read(bs, data->emblem)) return false;
  if (!bs.ReadCompressed(data->unknownByte)) return false;
  if (!DecodeString(bs, data->unknownTag1)) return false;
  if (!bs.ReadCompressed(data->unknownTag1Value)) return false;
  if (!DecodeString(bs, data->unknownTag2)) return false;
  if (!bs.ReadCompressed(data->unknownTag2Value)) return false;
  if (!bs.Read(bit)) return false;
  data->unknownFlag1 = bit;
  if (!bs.Read(bit)) return false;
  data->unknownFlag2 = bit;

  return true;
}

void ObjectDetailsPacketSerializer::Write(
    RakNet::BitStream& bs, const ObjectDetailsPacket* data) const {
  FactionEmblemInteropSerializer emblemSerializer;

  bs.WriteCompressed(data->objectId);
  bs.WriteCompressed(data->type);
  bs.Write(data->hasDetails != 0);

  if (!data->hasDetails) {
    bs.WriteCompressed(data->playerId);
    return;
  }

  EncodeString(bs, data->name);
  EncodeString(bs, data->unknownString32);
  EncodeString(bs, data->unknownString64);
  emblemSerializer.Write(bs, data->emblem);
  bs.WriteCompressed(data->unknownByte);
  EncodeString(bs, data->unknownTag1);
  bs.WriteCompressed(data->unknownTag1Value);
  EncodeString(bs, data->unknownTag2);
  bs.WriteCompressed(data->unknownTag2Value);
  bs.Write(data->unknownFlag1 != 0);
  bs.Write(data->unknownFlag2 != 0);
}

}  // namespace FOMNetwork
