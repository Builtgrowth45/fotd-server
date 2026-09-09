using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Packets.RakNet;
using FOMServer.Shared.Interop.FOMNetwork.Structs;
using FOMServer.World.Application.Players;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.Players.Registration;
using FOMServer.World.Core.World;
using FOMServer.World.Tests.Factories;
using Microsoft.Extensions.Time.Testing;

namespace FOMServer.World.Tests.Players
{
    public class PlayerBroadcastTests
    {
        [Fact]
        public void BroadcastToWorld_ReachesEveryPlayer()
        {
            var fixture = new BroadcastFixture();
            fixture.AddPlayer(1);
            fixture.AddPlayer(2);
            fixture.AddPlayer(3);

            // A writer passed by reference cannot be a using variable, so ownership
            // is released by hand.
            var writer = new PacketWriter<ConnectionRequestAcceptedPacket>();
            try
            {
                Assert.Equal(3, fixture.Registry.BroadcastToWorld(ref writer));

                var packet = writer.Build();
                Assert.Equal(3, packet.NetworkAddresses.Length);
                packet.Release();
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void BroadcastToWorld_ExcludesTheNamedPlayer()
        {
            var fixture = new BroadcastFixture();
            var excluded = fixture.AddPlayer(1);
            var included = fixture.AddPlayer(2);

            var writer = new PacketWriter<ConnectionRequestAcceptedPacket>();
            try
            {
                Assert.Equal(1, fixture.Registry.BroadcastToWorld(ref writer, excluded.Id));

                var packet = writer.Build();
                var addresses = packet.NetworkAddresses.ToArray();
                Assert.Single(addresses);
                Assert.Equal(included.Address, addresses[0]);
                packet.Release();
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void BroadcastToWorld_NoPlayers_LeavesWriterWithoutDestinations()
        {
            var fixture = new BroadcastFixture();

            var writer = new PacketWriter<ConnectionRequestAcceptedPacket>();
            try
            {
                Assert.Equal(0, fixture.Registry.BroadcastToWorld(ref writer));
                Assert.False(writer.HasDestinations);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void BroadcastToRadius_OnlyReachesPlayersInRange()
        {
            var fixture = new BroadcastFixture();
            fixture.AddPlayer(1, x: 0);
            fixture.AddPlayer(2, x: 50);
            fixture.AddPlayer(3, x: 5000);

            Assert.Equal(2, CountInRadius(fixture, Position(0, 0, 0), 100));
        }

        [Fact]
        public void BroadcastToRadius_IncludesPlayerExactlyOnTheBoundary()
        {
            var fixture = new BroadcastFixture();
            fixture.AddPlayer(1, x: 100);

            Assert.Equal(1, CountInRadius(fixture, Position(0, 0, 0), 100));
        }

        [Fact]
        public void BroadcastToRadius_MeasuresAcrossAllThreeAxes()
        {
            var fixture = new BroadcastFixture();

            // Inside 100 on each axis alone, but not once the axes combine.
            fixture.AddPlayer(1, x: 80, y: 80, z: 80);

            Assert.Equal(0, CountInRadius(fixture, Position(0, 0, 0), 100));
        }

        [Fact]
        public void BroadcastToRadius_ExcludesTheNamedPlayer()
        {
            var fixture = new BroadcastFixture();
            var excluded = fixture.AddPlayer(1, x: 0);
            fixture.AddPlayer(2, x: 10);

            Assert.Equal(1, CountInRadius(fixture, Position(0, 0, 0), 100, excluded.Id));
        }

        private static int CountInRadius(
            BroadcastFixture fixture,
            ServerPosition origin,
            ushort radius,
            uint? excludePlayerId = null
        )
        {
            var writer = new PacketWriter<ConnectionRequestAcceptedPacket>();
            try
            {
                return fixture.Registry.BroadcastToRadius(ref writer, origin, radius, excludePlayerId);
            }
            finally
            {
                writer.Dispose();
            }
        }

        private static ServerPosition Position(short x, short y, short z)
        {
            var position = new ServerPosition();
            position.ApplyUpdate(
                new PositionInterop
                {
                    X = x,
                    Y = y,
                    Z = z,
                }
            );
            return position;
        }

        private sealed class BroadcastFixture
        {
            public BroadcastFixture()
            {
                RegistrationFactory
                    .Setup(f => f.Create(It.IsAny<Player>()))
                    .Returns(new Mock<IPlayerRegistration>().Object);

                Persistence
                    .Setup(p => p.WaitForPersistence(It.IsAny<IPersistable>(), It.IsAny<Action>()))
                    .Callback<IPersistable, Action>((_, cb) => cb());

                Registry = new PlayerRegistry(Loader.Object, RegistrationFactory.Object, Time, Persistence.Object);
            }

            public Mock<IPlayerLoader> Loader { get; } = new();

            public Mock<IPlayerRegistrationFactory> RegistrationFactory { get; } = new();

            public Mock<IPersistenceService> Persistence { get; } = new();

            public FakeTimeProvider Time { get; } = new(DateTimeOffset.UnixEpoch);

            public PlayerRegistry Registry { get; }

            public Player AddPlayer(uint id, short x = 0, short y = 0, short z = 0)
            {
                Loader.Setup(l => l.Load(id)).Returns(TestPlayerBuilder.Create(id).Build());

                var binary = 0x0100007F + id;
                Registry.PrepareForClient(id, binary);

                var player =
                    Registry.ClaimForClient(id, new NetworkAddress { BinaryAddress = binary, Port = 7777 })
                    ?? throw new InvalidOperationException($"Player {id} was not claimed");

                player.Position.ApplyUpdate(
                    new PositionInterop
                    {
                        X = x,
                        Y = y,
                        Z = z,
                    }
                );

                return player;
            }
        }
    }
}
