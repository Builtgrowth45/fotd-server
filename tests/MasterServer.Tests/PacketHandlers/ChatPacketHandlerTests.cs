using System.Runtime.InteropServices;
using FOMServer.Master.Application.PacketHandlers;
using FOMServer.Master.Application.Players;
using FOMServer.Master.Core.Networking;
using FOMServer.Master.Core.Players;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using Microsoft.Extensions.Logging.Abstractions;

namespace FOMServer.Master.Tests.PacketHandlers
{
    public class ChatPacketHandlerTests
    {
        private const uint PlayerId = 42;
        private const string PlayerName = "Vera Kade";

        [Fact]
        public void Chat_UsesTheCharacterNameFromTheSession()
        {
            var fixture = new Fixture();
            var session = fixture.LoggedInSession();

            fixture.Handle(session.Address, new ChatPacket { Message = "hello" });

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(PlayerName, capture.SenderName);
            Assert.Equal(PlayerId, capture.SenderId);
            Assert.Equal("hello", capture.Message);
        }

        [Fact]
        public void Chat_IgnoresTheNameAndIdTheClientClaims()
        {
            var fixture = new Fixture();
            var session = fixture.LoggedInSession();

            fixture.Handle(
                session.Address,
                new ChatPacket
                {
                    SenderId = 999,
                    SenderName = "Impostor",
                    Message = "hi",
                }
            );

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(PlayerName, capture.SenderName);
            Assert.Equal(PlayerId, capture.SenderId);
        }

        [Fact]
        public void Chat_BeforeLoginCompletes_SendsNothing()
        {
            var fixture = new Fixture();

            // Registered and mid-login, but no character attached yet.
            var session = fixture.ClientRegistry.Register(Address());
            fixture.ClientRegistry.BeginLogin(session, PlayerId);

            fixture.Handle(session.Address, new ChatPacket { Message = "too early" });

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void Chat_FromUnknownSession_SendsNothing()
        {
            var fixture = new Fixture();
            fixture.LoggedInSession();

            fixture.Handle(
                new NetworkAddress { BinaryAddress = 0x0A0A0A0A, Port = 1234 },
                new ChatPacket { Message = "ghost" }
            );

            Assert.Empty(fixture.Sender.Sends);
        }

        [Fact]
        public void Chat_PassesThroughChannelAndTarget()
        {
            var fixture = new Fixture();
            var session = fixture.LoggedInSession();

            fixture.Handle(
                session.Address,
                new ChatPacket
                {
                    Channel = ChatChannel.Private,
                    TargetId = 7,
                    ChatStyle = 3,
                    Message = "psst",
                }
            );

            var capture = Assert.Single(fixture.Sender.Sends);
            Assert.Equal(ChatChannel.Private, capture.Channel);
            Assert.Equal(7u, capture.TargetId);
            Assert.Equal((byte)3, capture.ChatStyle);
        }

        private static NetworkAddress Address()
        {
            return new NetworkAddress { BinaryAddress = 0x0100007F, Port = 7777 };
        }

        private sealed record Capture(
            ChatChannel Channel,
            uint SenderId,
            uint TargetId,
            byte ChatStyle,
            string SenderName,
            string Message
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
                        chat.ChatStyle,
                        chat.SenderName,
                        chat.Message
                    )
                );
                packet.Release();
            }

            public void CloseConnection(in NetworkAddress address)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class Fixture
        {
            private readonly ChatPacketHandler _handler;
            private readonly PlayerRegistry _playerRegistry;

            public Fixture()
            {
                var persistence = new Mock<IPersistenceService>();
                persistence
                    .Setup(p => p.WaitForPersistence(It.IsAny<IPersistable>(), It.IsAny<Action>()))
                    .Callback<IPersistable, Action>((_, cb) => cb());

                _playerRegistry = new PlayerRegistry(persistence.Object);
                _handler = new ChatPacketHandler(ClientRegistry, Sender, NullLogger<ChatPacketHandler>.Instance);
            }

            public ClientRegistry ClientRegistry { get; } = new();

            public CapturingSender Sender { get; } = new();

            public ClientSession LoggedInSession()
            {
                var session = ClientRegistry.Register(Address());
                ClientRegistry.BeginLogin(session, PlayerId);
                _playerRegistry.Login(session, PlayerName);
                return session;
            }

            public void Handle(NetworkAddress sender, ChatPacket packet)
            {
                _handler.Handle(sender, packet);
            }
        }
    }
}
