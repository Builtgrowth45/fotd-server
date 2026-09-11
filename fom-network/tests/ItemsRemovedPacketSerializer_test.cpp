#include "../src/FOMDataSerializer.h"

#pragma warning(push)
#pragma warning(disable : 26495)

#include <gtest/gtest.h>
#pragma warning(pop)

#include <fom-network/packets/ItemsRemovedPacket.h>

#include <cstring>
#include <memory>

using namespace FOMNetwork;

namespace {

void ExpectSameBits(RakNet::BitStream& expected, RakNet::BitStream& actual) {
  ASSERT_EQ(expected.GetNumberOfBitsUsed(), actual.GetNumberOfBitsUsed());
  EXPECT_EQ(0, std::memcmp(expected.GetData(), actual.GetData(),
                           BITS_TO_BYTES(expected.GetNumberOfBitsUsed())));
}

// The item list makes the packet far too large to hold on the stack.
std::unique_ptr<ItemsRemovedPacket> MakePacket() {
  return std::unique_ptr<ItemsRemovedPacket>(new ItemsRemovedPacket());
}

}  // namespace

TEST(ItemsRemovedPacketSerializer, WritesInTheClientsOrder) {
  auto packet = MakePacket();
  packet->playerId = 42;
  packet->removeType = 1;
  packet->numItemIds = 2;
  packet->itemIds[0] = 100;
  packet->itemIds[1] = 200;

  RakNet::BitStream actual;
  ASSERT_TRUE(FOMDataSerializer::Write(actual, Enum::ID_ITEMS_REMOVED,
                                       (const uint8_t*)packet.get()));

  RakNet::BitStream expected;
  expected.WriteCompressed((uint32_t)42);
  expected.WriteCompressed((uint8_t)1);
  expected.WriteCompressed((uint16_t)2);
  expected.WriteCompressed((uint32_t)100);
  expected.WriteCompressed((uint32_t)200);

  ExpectSameBits(expected, actual);
}

TEST(ItemsRemovedPacketSerializer, ClampsTheItemCount) {
  auto packet = MakePacket();
  packet->numItemIds = BufferSizes::MAX_ITEM_LIST_SIZE + 1;

  RakNet::BitStream actual;
  ASSERT_TRUE(FOMDataSerializer::Write(actual, Enum::ID_ITEMS_REMOVED,
                                       (const uint8_t*)packet.get()));

  RakNet::BitStream expected;
  expected.WriteCompressed((uint32_t)0);
  expected.WriteCompressed((uint8_t)0);
  expected.WriteCompressed((uint16_t)BufferSizes::MAX_ITEM_LIST_SIZE);
  for (int i = 0; i < BufferSizes::MAX_ITEM_LIST_SIZE; ++i)
    expected.WriteCompressed((uint32_t)0);

  ExpectSameBits(expected, actual);
}
