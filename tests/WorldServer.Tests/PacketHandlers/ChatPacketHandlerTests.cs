using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
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
    public class ChatPacketHandlerTests
    {
        [Fact]
        public void General_ReachesEveryPlayerIncludingTheAuthor()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);
            fixture.AddPlayer(2);
            fixture.AddPlayer(3);

            fixture.Send(author, ChatChannel.General, "hello world");

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(3, capture.Addresses.Length);
            Assert.Equal("hello world", capture.Message);
            Assert.Equal(ChatChannel.General, capture.Channel);
        }

        [Fact]
        public void General_UsesTheServersNameNotTheClaimedOne()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);

            fixture.Send(author, ChatChannel.General, "hi", claimedSenderName: "Impostor", claimedSenderId: 999);

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(author.Name, capture.SenderName);
            Assert.Equal(author.Id, capture.SenderId);
        }

        [Fact]
        public void Private_ReachesTargetAndAuthorOnly()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);
            var target = fixture.AddPlayer(2);
            fixture.AddPlayer(3);

            fixture.Send(author, ChatChannel.Private, "psst", targetId: target.Id);

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(2, capture.Addresses.Length);
            Assert.Contains(target.Address, capture.Addresses);
            Assert.Contains(author.Address, capture.Addresses);
            Assert.Equal(target.Id, capture.TargetId);
        }

        [Fact]
        public void Private_ToSelf_SendsOneCopy()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);

            fixture.Send(author, ChatChannel.Private, "note to self", targetId: author.Id);

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Single(capture.Addresses);
        }

        [Fact]
        public void Private_UnknownTarget_SendsNothing()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);

            fixture.Send(author, ChatChannel.Private, "anyone there", targetId: 4242);

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void UnhandledChannel_SendsNothing()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);

            fixture.Send(author, ChatChannel.Faction, "faction talk");

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void EmptyMessage_SendsNothing()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);

            fixture.Send(author, ChatChannel.General, "");

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void UnknownSender_SendsNothing()
        {
            var fixture = new ChatFixture();
            fixture.AddPlayer(1);

            fixture.SendFrom(
                new NetworkAddress { BinaryAddress = 0x0A0A0A0A, Port = 1234 },
                ChatChannel.General,
                "ghost"
            );

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void PositionCommand_RepliesOnlyToTheAuthor()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);
            fixture.AddPlayer(2);

            fixture.Send(author, ChatChannel.General, "!pos");

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Single(capture.Addresses);
            Assert.Equal(author.Address, capture.Addresses[0]);
            Assert.Equal(ChatChannel.System, capture.Channel);
            Assert.Contains($"Player {author.Id}", capture.Message);
        }

        [Fact]
        public void UnknownCommand_SendsNothingAndIsNotBroadcast()
        {
            var fixture = new ChatFixture();
            var author = fixture.AddPlayer(1);
            fixture.AddPlayer(2);

            fixture.Send(author, ChatChannel.General, "!nope");

            Assert.Empty(fixture.Sender.Sends);
        }

        private sealed record Capture(
            ChatChannel Channel,
            uint SenderId,
            uint TargetId,
            string SenderName,
            string Message,
            NetworkAddress[] Addresses
        );

        private sealed class CapturingSender : IClientPacketSender
        {
            public List<Capture> Sends { get; } = [];

            public void Send(in QueuePacket packet)
            {
                // Decode immediately: the packet's buffer is pooled and released here.
                var chat = MemoryMarshal.Read<ChatPacket>(packet.Data);

                Sends.Add(
                    new Capture(
                        chat.Channel,
                        chat.SenderId,
                        chat.TargetId,
                        chat.SenderName,
                        chat.Message,
                        packet.NetworkAddresses.ToArray()
                    )
                );
                packet.Release();
            }

            public void CloseConnection(in NetworkAddress address)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ChatFixture
        {
            private readonly PlayerRegistry _registry;
            private readonly ChatPacketHandler _handler;

            public ChatFixture()
            {
                var registrationFactory = new Mock<IPlayerRegistrationFactory>();
                registrationFactory
                    .Setup(f => f.Create(It.IsAny<Player>()))
                    .Returns(new Mock<IPlayerRegistration>().Object);

                var persistence = new Mock<IPersistenceService>();
                persistence
                    .Setup(p => p.WaitForPersistence(It.IsAny<IPersistable>(), It.IsAny<Action>()))
                    .Callback<IPersistable, Action>((_, cb) => cb());

                _registry = new PlayerRegistry(
                    Loader.Object,
                    registrationFactory.Object,
                    new FakeTimeProvider(DateTimeOffset.UnixEpoch),
                    persistence.Object
                );

                _handler = new ChatPacketHandler(_registry, Sender, NullLogger<ChatPacketHandler>.Instance);
            }

            public Mock<IPlayerLoader> Loader { get; } = new();

            public CapturingSender Sender { get; } = new();

            public Player AddPlayer(uint id)
            {
                Loader.Setup(l => l.Load(id)).Returns(TestPlayerBuilder.Create(id).Build());

                var binary = 0x0100007F + id;
                _registry.PrepareForClient(id, binary);

                return _registry.ClaimForClient(id, new NetworkAddress { BinaryAddress = binary, Port = 7777 })
                    ?? throw new InvalidOperationException($"Player {id} was not claimed");
            }

            public void Send(
                Player author,
                ChatChannel channel,
                string message,
                uint targetId = 0,
                string? claimedSenderName = null,
                uint? claimedSenderId = null
            )
            {
                var packet = new ChatPacket
                {
                    Channel = channel,
                    SenderId = claimedSenderId ?? author.Id,
                    TargetId = targetId,
                    ChatStyle = 0,
                    SenderName = claimedSenderName ?? author.Name,
                    Message = message,
                };

                _handler.Handle(author.Address, packet);
            }

            public void SendFrom(NetworkAddress sender, ChatChannel channel, string message)
            {
                var packet = new ChatPacket { Channel = channel, Message = message };

                _handler.Handle(sender, packet);
            }
        }
    }
}
