using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.World.Application.PacketHandlers;
using FOMServer.World.Application.Players;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;
using FOMServer.World.Core.Players.Registration;
using FOMServer.World.Tests.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FOMServer.World.Tests.PacketHandlers
{
    public class ObjectDetailsPacketHandlerTests
    {
        private const uint RequesterId = 1;
        private const uint TargetId = 2;

        [Fact]
        public void Request_ForAPlayer_RepliesToTheRequesterWithTheirDetails()
        {
            var fixture = new HandlerFixture();
            var requester = fixture.AddPlayer(RequesterId, "Requester");
            var target = fixture.AddPlayer(TargetId, "Target");

            fixture.Handler.Handle(requester.Address, Request(target.Id, requester.Id));

            var reply = Assert.Single(fixture.Sender.Replies);
            Assert.Equal(requester.Address, reply.Destination);
            Assert.Equal(target.Id, reply.Packet.ObjectId);
            Assert.Equal(1, reply.Packet.Type);
            Assert.Equal(1, reply.Packet.HasDetails);
            Assert.Equal("Target", reply.Packet.Name);
            Assert.Equal(3, reply.Packet.UnknownByte);
        }

        [Fact]
        public void Request_ForUnknownObject_SendsNothing()
        {
            var fixture = new HandlerFixture();
            var requester = fixture.AddPlayer(RequesterId, "Requester");

            fixture.Handler.Handle(requester.Address, Request(4242, requester.Id));

            Assert.Empty(fixture.Sender.Replies);
        }

        [Fact]
        public void Request_FromUnregisteredClient_SendsNothing()
        {
            var fixture = new HandlerFixture();
            var target = fixture.AddPlayer(TargetId, "Target");

            var stranger = new NetworkAddress { BinaryAddress = 0x0A0A0A0A, Port = 1234 };
            fixture.Handler.Handle(stranger, Request(target.Id, RequesterId));

            Assert.Empty(fixture.Sender.Replies);
        }

        [Fact]
        public void Request_CarryingDetails_IsIgnored()
        {
            var fixture = new HandlerFixture();
            var requester = fixture.AddPlayer(RequesterId, "Requester");
            var target = fixture.AddPlayer(TargetId, "Target");

            var packet = Request(target.Id, requester.Id);
            packet.HasDetails = 1;
            fixture.Handler.Handle(requester.Address, packet);

            Assert.Empty(fixture.Sender.Replies);
        }

        [Fact]
        public void Request_WithOtherType_IsIgnored()
        {
            var fixture = new HandlerFixture();
            var requester = fixture.AddPlayer(RequesterId, "Requester");
            var target = fixture.AddPlayer(TargetId, "Target");

            var packet = Request(target.Id, requester.Id);
            packet.Type = 2;
            fixture.Handler.Handle(requester.Address, packet);

            Assert.Empty(fixture.Sender.Replies);
        }

        private static ObjectDetailsPacket Request(uint objectId, uint playerId)
        {
            return new ObjectDetailsPacket
            {
                ObjectId = objectId,
                Type = 1,
                PlayerId = playerId,
            };
        }

        private readonly record struct Reply(NetworkAddress Destination, ObjectDetailsPacket Packet);

        private sealed class CapturingSender : IClientPacketSender
        {
            public List<Reply> Replies { get; } = [];

            public void Send(in QueuePacket packet)
            {
                // Copied out straight away since the buffer goes back to its pool.
                Replies.Add(
                    new Reply(packet.NetworkAddresses[0], MemoryMarshal.Read<ObjectDetailsPacket>(packet.Data))
                );
                packet.Release();
            }

            public void CloseConnection(in NetworkAddress address)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class HandlerFixture
        {
            public HandlerFixture()
            {
                var registrationFactory = new Mock<IPlayerRegistrationFactory>();
                registrationFactory
                    .Setup(f => f.Create(It.IsAny<Player>()))
                    .Returns(new Mock<IPlayerRegistration>().Object);

                Registry = new PlayerRegistry(
                    Loader.Object,
                    registrationFactory.Object,
                    new FakeTimeProvider(DateTimeOffset.UnixEpoch),
                    new Mock<IPersistenceService>().Object
                );

                Handler = new ObjectDetailsPacketHandler(
                    Registry,
                    Sender,
                    NullLogger<ObjectDetailsPacketHandler>.Instance
                );
            }

            public Mock<IPlayerLoader> Loader { get; } = new();

            public PlayerRegistry Registry { get; }

            public CapturingSender Sender { get; } = new();

            public ObjectDetailsPacketHandler Handler { get; }

            public Player AddPlayer(uint id, string name)
            {
                Loader.Setup(l => l.Load(id)).Returns(new TestPlayerBuilder(id, name).Build());

                var binaryAddress = 0x0100007F + id;
                Registry.PrepareForClient(id, binaryAddress);

                var address = new NetworkAddress { BinaryAddress = binaryAddress, Port = 7777 };
                return Registry.ClaimForClient(id, address)
                    ?? throw new InvalidOperationException($"Player {id} could not be claimed");
            }
        }
    }
}
