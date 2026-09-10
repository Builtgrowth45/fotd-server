namespace FOMServer.Master.Core.Players
{
    internal interface IPlayerRegistry
    {
        Player? Get(uint playerId);

        Player Login(ClientSession session, string name);

        void Logout(Player player);
    }
}
