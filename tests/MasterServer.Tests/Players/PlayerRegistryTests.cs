using FOMServer.Master.Application.Players;
using FOMServer.Master.Core.Players;
using FOMServer.Shared.Core.Persistence;
using FOMServer.Shared.Interop.FOMNetwork;

namespace FOMServer.Master.Tests.Players
{
    public class PlayerRegistryTests
    {
        private const uint PlayerId = 42;
        private const string PlayerName = "Test Player";

        [Fact]
        public void Login_ThenLogout_RemovesThePlayer()
        {
            var fixture = new Fixture();
            var session = fixture.BeginLogin();

            var player = fixture.Registry.Login(session, PlayerName);
            Assert.Same(player, fixture.Registry.Get(PlayerId));

            fixture.Registry.Logout(player);
            Assert.Null(fixture.Registry.Get(PlayerId));
        }

        [Fact]
        public void Disconnect_BeforeLoginAttachesThePlayer_DoesNotOrphanIt()
        {
            var fixture = new Fixture();
            var session = fixture.BeginLogin();

            // The client leaves while the player is still being built, so the
            // session never gets its player attached.
            fixture.ClientRegistry.Unregister(session);
            Assert.Null(session.Player);

            fixture.Registry.Login(session, PlayerName);

            // The login notices the session is gone and releases what it built.
            Assert.Null(fixture.Registry.Get(PlayerId));
        }

        [Fact]
        public void Disconnect_AfterLoginAttachesThePlayer_RemovesIt()
        {
            var fixture = new Fixture();
            var session = fixture.BeginLogin();
            fixture.Registry.Login(session, PlayerName);

            Assert.NotNull(fixture.Registry.Get(PlayerId));

            fixture.ClientRegistry.Unregister(session);
            fixture.Registry.LogoutSession(session);

            Assert.Null(fixture.Registry.Get(PlayerId));
        }

        [Fact]
        public void LogoutSession_WithPlayerInFlight_FindsItById()
        {
            var fixture = new Fixture();
            var session = fixture.BeginLogin();

            // Registered against the id, but the session was never told about it.
            var player = fixture.Registry.Login(session, PlayerName);
            Assert.NotNull(fixture.Registry.Get(PlayerId));

            var orphaned = new OrphanSession(session.Address, PlayerId);
            fixture.Registry.LogoutSession(orphaned);

            Assert.Null(fixture.Registry.Get(PlayerId));
            Assert.Equal(PlayerId, player.Id);
        }

        [Fact]
        public void LogoutSession_WithNothingToRelease_DoesNothing()
        {
            var fixture = new Fixture();
            var session = new ClientSession(Address());

            fixture.Registry.LogoutSession(session);

            Assert.Null(fixture.Registry.Get(PlayerId));
        }

        private static NetworkAddress Address()
        {
            return new NetworkAddress { BinaryAddress = 0x0100007F, Port = 7777 };
        }

        /// <summary>
        /// A session that knows its player id but never had the player attached,
        /// which is the state a disconnect mid-login leaves behind.
        /// </summary>
        private sealed class OrphanSession : ClientSession
        {
            public OrphanSession(NetworkAddress address, uint playerId)
                : base(address)
            {
                BeginLogin(playerId);
            }
        }

        private sealed class Fixture
        {
            public Fixture()
            {
                Persistence
                    .Setup(p => p.WaitForPersistence(It.IsAny<IPersistable>(), It.IsAny<Action>()))
                    .Callback<IPersistable, Action>((_, cb) => cb());

                Registry = new PlayerRegistry(Persistence.Object);
            }

            public Mock<IPersistenceService> Persistence { get; } = new();

            public ClientRegistry ClientRegistry { get; } = new();

            public PlayerRegistry Registry { get; }

            public ClientSession BeginLogin()
            {
                var session = ClientRegistry.Register(Address());
                ClientRegistry.BeginLogin(session, PlayerId);
                return session;
            }
        }
    }
}
