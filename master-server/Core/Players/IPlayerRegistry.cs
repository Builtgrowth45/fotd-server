namespace FOMServer.Master.Core.Players
{
    internal interface IPlayerRegistry
    {
        Player? Get(uint playerId);

        Player Login(ClientSession session, string name);

        void Logout(Player player);

        /// <summary>
        /// Logs out whichever player belongs to a session that has gone away.
        /// </summary>
        /// <remarks>
        /// Covers a login that had not finished attaching its player to the
        /// session, which would otherwise leave the player in the registry with
        /// nothing owning it.
        /// </remarks>
        void LogoutSession(ClientSession session);
    }
}
