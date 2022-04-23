
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins {
    [Info("Welcomer", "Mattdokn", 1.0)]
    class Welcomer : RustPlugin {
        void OnUserConnected(IPlayer player) {
            Server.Broadcast($"<color=#6b42ff>{player.Name}</color> has connected to the server!", ulong.Parse(player.Id));
        }
        [ChatCommand("pop")]
        private void DiscordCommand(BasePlayer player, string command, string[] args) {
            ConVar.Chat.ChatEntry entry = new ConVar.Chat.ChatEntry();
            /**
             * public ChatChannel Channel { get; set; }
            public string Message { get; set; }
            public string UserId { get; set; }
            public string Username { get; set; }
            public string Color { get; set; }
            public int Time { get; set; }
             * 
             */
            global::ConsoleNetwork.BroadcastToAllClients("chat.add2", new object[]
                {
                    0,
                    player.userID,
                    "Why hello there.",
                    player.displayName,
                    "#fa5",
                    1f
                });
            player.ChatMessage($"Server Pop: {BasePlayer.activePlayerList.Count}");
        }

        void OnEntitySpawned(BaseNetworkable entity) {
            if (entity.PrefabName.Contains("keycard.entity.prefab")) {
                Puts("Killed card.");
                entity.Kill();
            }
        }
    }
}
