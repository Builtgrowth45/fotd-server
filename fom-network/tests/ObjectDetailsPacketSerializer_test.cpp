#include "../src/FOMDataSerializer.h"

#pragma warning(push)
#pragma warning(disable : 26495)

#include <gtest/gtest.h>
#pragma warning(pop)

#include <fom-network/packets/ObjectDetailsPacket.h>

#include <cstring>

using namespace FOMNetwork;

namespace {

void ExpectSameBits(RakNet::BitStream& expected, RakNet::BitStream& actual) {
  ASSERT_EQ(expected.GetNumberOfBitsUsed(), actual.GetNumberOfBitsUsed());
  EXPECT_EQ(0, std::memcmp(expected.GetData(), actual.GetData(),
                           BITS_TO_BYTES(expected.GetNumberOfBitsUsed())));
}

}  // namespace

TEST(ObjectDetailsPacketSerializer, WritesARequestInTheClientsOrder) {
  ObjectDetailsPacket packet{};
  packet.objectId = 1234;
  packet.type = 1;
  packet.hasDetails = 0;
  packet.playerId = 42;

  RakNet::BitStream actual;
  ASSERT_TRUE(FOMDataSerializer::Write(actual, Enum::ID_OBJECT_DETAILS,
                                       (const uint8_t*)&packet));

  RakNet::BitStream expected;
  expected.WriteCompressed((uint32_t)1234);
  expected.WriteCompressed((uint8_t)1);
  expected.Write0();
  expected.WriteCompressed((uint32_t)42);

  ExpectSameBits(expected, actual);
}

TEST(ObjectDetailsPacketSerializer, RoundTripsARequest) {
  ObjectDetailsPacket in{};
  in.objectId = 1234;
  in.type = 1;
  in.playerId = 42;

  RakNet::BitStream bs;
  ASSERT_TRUE(FOMDataSerializer::Write(bs, Enum::ID_OBJECT_DETAILS,
                                       (const uint8_t*)&in));

  ObjectDetailsPacket out{};
  ASSERT_TRUE(
      FOMDataSerializer::Read(bs, Enum::ID_OBJECT_DETAILS, (uint8_t*)&out));
  EXPECT_EQ(0, std::memcmp(&in, &out, sizeof(ObjectDetailsPacket)));
}

TEST(ObjectDetailsPacketSerializer, RoundTripsDetails) {
  ObjectDetailsPacket in{};
  in.objectId = 1234;
  in.type = 1;
  in.hasDetails = 1;
  std::memcpy(in.name, "Somebody", sizeof("Somebody"));
  std::memcpy(in.unknownString32, "Thirty Two", sizeof("Thirty Two"));
  std::memcpy(in.unknownString64, "Sixty Four", sizeof("Sixty Four"));
  in.emblem.staticEmblemId = 7;
  in.emblem.layers[0].shape = 3;
  in.emblem.layers[0].offsetX = 10;
  in.emblem.layers[0].offsetY = 20;
  in.emblem.layers[0].scaleWidth = 50;
  in.emblem.layers[0].scaleHeight = 60;
  in.emblem.layers[0].rotation = 90;
  in.emblem.layers[0].red = 1;
  in.emblem.layers[0].green = 2;
  in.emblem.layers[0].blue = 3;
  in.unknownByte = 3;
  std::memcpy(in.unknownTag1, "abc", sizeof("abc"));
  in.unknownTag1Value = 5;
  std::memcpy(in.unknownTag2, "xyz", sizeof("xyz"));
  in.unknownTag2Value = 6;
  in.unknownFlag1 = 1;
  in.unknownFlag2 = 0;

  RakNet::BitStream bs;
  ASSERT_TRUE(FOMDataSerializer::Write(bs, Enum::ID_OBJECT_DETAILS,
                                       (const uint8_t*)&in));

  ObjectDetailsPacket out{};
  ASSERT_TRUE(
      FOMDataSerializer::Read(bs, Enum::ID_OBJECT_DETAILS, (uint8_t*)&out));
  EXPECT_EQ(0, std::memcmp(&in, &out, sizeof(ObjectDetailsPacket)));
}

TEST(ObjectDetailsPacketSerializer, RejectsATruncatedStream) {
  RakNet::BitStream bs;
  bs.WriteCompressed((uint32_t)1234);

  ObjectDetailsPacket out{};
  EXPECT_FALSE(
      FOMDataSerializer::Read(bs, Enum::ID_OBJECT_DETAILS, (uint8_t*)&out));
}
