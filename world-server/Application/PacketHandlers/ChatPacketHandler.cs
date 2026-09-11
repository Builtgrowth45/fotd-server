using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Networking;
using FOMServer.Shared.Core.PacketHandlers;
using FOMServer.Shared.Interop.FOMNetwork;
using FOMServer.Shared.Interop.FOMNetwork.Enums;
using FOMServer.Shared.Interop.FOMNetwork.Packets;
using FOMServer.Shared.Metadata;
using FOMServer.World.Core.Networking;
using FOMServer.World.Core.Players;

namespace FOMServer.World.Application.PacketHandlers
{
    [PacketHandler]
    internal class ChatPacketHandler : PacketHandlerBase<ChatPacket>
    {
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IClientPacketSender _clientPacketSender;
        private readonly ILogger<ChatPacketHandler> _logger;

        public ChatPacketHandler(
            IPlayerRegistry playerRegistry,
            IClientPacketSender clientPacketSender,
            ILogger<ChatPacketHandler> logger
        )
        {
            _playerRegistry = playerRegistry;
            _clientPacketSender = clientPacketSender;
            _logger = logger;
        }

        public override void Handle(NetworkAddress sender, in ChatPacket p)
        {
            var player = _playerRegistry.Get(sender);
            if (player is null)
            {
                _logger.LogWarning("Received unexpected packet for player {PlayerId}", p.SenderId);
                return;
            }

            // Commands never reach other players, so they are handled before routing.
            if (TryHandleCommand(sender, player, p.Message))
            {
                return;
            }

            var message = p.Message;
            if (message.Length == 0)
            {
                return;
            }

            switch (p.Channel)
            {
                case ChatChannel.General:
                    SendToWorld(player, p.Channel, p.ChatStyle, message);
                    break;

                case ChatChannel.Private:
                    SendToPlayer(player, p.TargetId, p.ChatStyle, message);
                    break;

                default:
                    _logger.LogDebug("Chat channel {Channel} is not handled yet", p.Channel);
                    break;
            }
        }

        /// <summary>
        /// Sends a message to everyone on this world, the sender included.
        /// </summary>
        private void SendToWorld(Player author, ChatChannel channel, byte chatStyle, string message)
        {
            // A writer passed by reference cannot be a using variable, so ownership
            // is released by hand.
            var writer = new PacketWriter<ChatPacket>();
            try
            {
                if (_playerRegistry.BroadcastToWorld(ref writer) == 0)
                {
                    return;
                }

                Fill(ref writer, author, channel, targetId: 0, chatStyle, message);
                _clientPacketSender.Send(writer.Build());
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Sends a message to a single player, echoing it back to the author.
        /// </summary>
        private void SendToPlayer(Player author, uint targetId, byte chatStyle, string message)
        {
            var target = _playerRegistry.Get(targetId);
            if (target is null)
            {
                // The recipient is offline or on another world. Relaying across
                // worlds runs through the master server, which does not carry
                // chat yet.
                _logger.LogDebug("Private chat target {TargetId} is not on this world", targetId);
                return;
            }

            var writer = new PacketWriter<ChatPacket>(target.Address);
            try
            {
                // The author sees their own private messages in the conversation.
                if (target.Id != author.Id)
                {
                    writer.AddDestination(author.Address);
                }

                Fill(ref writer, author, ChatChannel.Private, targetId, chatStyle, message);
                _clientPacketSender.Send(writer.Build());
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Fills in the outgoing packet, taking the sender's identity from the
        /// server rather than from anything the client claimed.
        /// </summary>
        private static void Fill(
            ref PacketWriter<ChatPacket> writer,
            Player author,
            ChatChannel channel,
            uint targetId,
            byte chatStyle,
            string message
        )
        {
            ref var data = ref writer.Data;
            data.Channel = channel;
            data.SenderId = author.Id;
            data.TargetId = targetId;
            data.ChatStyle = chatStyle;
            data.SenderName = author.Name;
            data.Message = message;
        }

        private bool TryHandleCommand(NetworkAddress sender, Player player, string message)
        {
            if (!message.StartsWith('!'))
            {
                return false;
            }

            var responseMessage = message[1..] switch
            {
                "pos" => $"Player {player.Id} ({player.Position})",
                _ => "",
            };

            if (responseMessage.Length == 0)
            {
                return true;
            }

            using var response = new PacketWriter<ChatPacket>(sender);
            ref var data = ref response.Data;
            data.Channel = ChatChannel.System;
            data.SenderId = player.Id;
            data.SenderName = player.Name;
            data.Message = responseMessage;
            _clientPacketSender.Send(response.Build());

            return true;
        }
    }
}
