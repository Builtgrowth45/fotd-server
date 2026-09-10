using FOMServer.Shared.Core.Persistence;

namespace FOMServer.Master.Core.Players
{
    internal class Player : IPersistable
    {
        private readonly ClientSession _session;

        public Player(uint id, string name, ClientSession session)
        {
            Id = id;
            Name = name;
            _session = session;
        }

        public event PersistableChangeHandler? PersistableChange;

        public uint Id { get; }

        /// <summary>
        /// The character's name, as it should appear to other players.
        /// </summary>
        public string Name { get; }
    }
}
