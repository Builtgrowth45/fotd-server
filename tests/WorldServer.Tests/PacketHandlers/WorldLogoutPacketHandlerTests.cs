using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.World.Application.PacketHandlers;
using FOMServer.World.Application.Players;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.Players.Registration;
using FOMServer.World.Tests.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FOMServer.World.Tests.PacketHandlers
{
    public class WorldLogoutPacketHandlerTests
    {
        [Fact]
        public void Logout_RemovesThePlayerFromTheWorld()
        {
            var fixture = new Fixture();
            var player = fixture.AddPlayer(1);

            fixture.Handle(player.Address, new WorldLogoutPacket { PlayerId = player.Id });

            Assert.Null(fixture.Registry.Get(player.Id));
        }

        [Fact]
        public void Logout_WhenChangingWorlds_StillLeavesThisWorld()
        {
            var fixture = new Fixture();
            var player = fixture.AddPlayer(1);

            fixture.Handle(player.Address, new WorldLogoutPacket { PlayerId = player.Id, IsChangingWorlds = 1 });

            Assert.Null(fixture.Registry.Get(player.Id));
        }

        [Fact]
        public void Logout_FromUnregisteredClient_DoesNothing()
        {
            var fixture = new Fixture();
            var player = fixture.AddPlayer(1);

            fixture.Handle(
                new NetworkAddress { BinaryAddress = 0x0A0A0A0A, Port = 1234 },
                new WorldLogoutPacket { PlayerId = player.Id }
            );

            Assert.NotNull(fixture.Registry.Get(player.Id));
        }

        [Fact]
        public void Logout_ClaimingAnotherPlayer_OnlyLogsOutTheSender()
        {
            var fixture = new Fixture();
            var sender = fixture.AddPlayer(1);
            var victim = fixture.AddPlayer(2);

            // The id in the packet is ignored in favour of the sender's own
            // registration, so a client cannot log anybody else out.
            fixture.Handle(sender.Address, new WorldLogoutPacket { PlayerId = victim.Id });

            Assert.Null(fixture.Registry.Get(sender.Id));
            Assert.NotNull(fixture.Registry.Get(victim.Id));
        }

        private sealed class Fixture
        {
            private readonly WorldLogoutPacketHandler _handler;

            public Fixture()
            {
                var registrationFactory = new Mock<IPlayerRegistrationFactory>();
                registrationFactory
                    .Setup(f => f.Create(It.IsAny<Player>()))
                    .Returns(new Mock<IPlayerRegistration>().Object);

                var persistence = new Mock<IPersistenceService>();
                persistence
                    .Setup(p => p.WaitForPersistence(It.IsAny<IPersistable>(), It.IsAny<Action>()))
                    .Callback<IPersistable, Action>((_, cb) => cb());

                Registry = new PlayerRegistry(
                    Loader.Object,
                    registrationFactory.Object,
                    new FakeTimeProvider(DateTimeOffset.UnixEpoch),
                    persistence.Object
                );

                _handler = new WorldLogoutPacketHandler(Registry, NullLogger<WorldLogoutPacketHandler>.Instance);
            }

            public Mock<IPlayerLoader> Loader { get; } = new();

            public PlayerRegistry Registry { get; }

            public Player AddPlayer(uint id)
            {
                Loader.Setup(l => l.Load(id)).Returns(TestPlayerBuilder.Create(id).Build());

                var binary = 0x0100007F + id;
                Registry.PrepareForClient(id, binary);

                return Registry.ClaimForClient(id, new NetworkAddress { BinaryAddress = binary, Port = 7777 })
                    ?? throw new InvalidOperationException($"Player {id} was not claimed");
            }

            public void Handle(NetworkAddress sender, WorldLogoutPacket packet)
            {
                _handler.Handle(sender, packet);
            }
        }
    }
}
