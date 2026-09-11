#include "../src/structs/faction/FactionEmblemLayerInteropSerializer.h"

#pragma warning(push)
#pragma warning(disable : 26495)

#include <gtest/gtest.h>
#pragma warning(pop)

#include <cstring>

using namespace FOMNetwork;

namespace {

void ExpectSameBits(RakNet::BitStream& expected, RakNet::BitStream& actual) {
  ASSERT_EQ(expected.GetNumberOfBitsUsed(), actual.GetNumberOfBitsUsed());
  EXPECT_EQ(0, std::memcmp(expected.GetData(), actual.GetData(),
                           BITS_TO_BYTES(expected.GetNumberOfBitsUsed())));
}

}  // namespace

TEST(FactionEmblemLayerInteropSerializer, WritesAScaledLayerAtTheOrigin) {
  FactionEmblemLayerInterop layer{};
  layer.scaleWidth = 40;
  layer.scaleHeight = 50;
  layer.rotation = 300;
  layer.red = 1;
  layer.green = 2;
  layer.blue = 3;

  RakNet::BitStream actual;
  FactionEmblemLayerInteropSerializer().Write(actual, layer);

  // Mirrors the client's FactionEmblemLayer::Write.
  RakNet::BitStream expected;
  expected.Write1();
  expected.WriteCompressed(layer.shape);
  expected.WriteCompressed(layer.offsetX);
  expected.WriteCompressed(layer.offsetY);
  expected.WriteBits(&layer.scaleWidth, 7);
  expected.WriteBits(&layer.scaleHeight, 7);
  expected.WriteBits((const unsigned char*)&layer.rotation, 9);
  expected.WriteCompressed(layer.red);
  expected.WriteCompressed(layer.green);
  expected.WriteCompressed(layer.blue);

  ExpectSameBits(expected, actual);
}

TEST(FactionEmblemLayerInteropSerializer, SkipsALayerMissingAScale) {
  FactionEmblemLayerInterop layer{};
  layer.shape = 5;
  layer.offsetX = 3;
  layer.offsetY = -2;
  layer.scaleWidth = 40;

  RakNet::BitStream actual;
  FactionEmblemLayerInteropSerializer().Write(actual, layer);

  RakNet::BitStream expected;
  expected.Write0();

  ExpectSameBits(expected, actual);
}

TEST(FactionEmblemLayerInteropSerializer, RoundTripsAScaledLayer) {
  FactionEmblemLayerInterop in{};
  in.offsetX = -4;
  in.offsetY = 7;
  in.scaleWidth = 40;
  in.scaleHeight = 50;
  in.rotation = 300;
  in.red = 1;
  in.green = 2;
  in.blue = 3;

  FactionEmblemLayerInteropSerializer serializer;
  RakNet::BitStream bs;
  serializer.Write(bs, in);

  FactionEmblemLayerInterop out{};
  ASSERT_TRUE(serializer.Read(bs, out));
  EXPECT_EQ(0, std::memcmp(&in, &out, sizeof(FactionEmblemLayerInterop)));
}
