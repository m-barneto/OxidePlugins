
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("DiscordReminder", "Mattdokn", 1.0)]
    class DiscordReminder : RustPlugin {
        class IgnoreList {
            public List<ulong> players = new List<ulong>();
        }
        IgnoreList ignoreList;

        void Loaded() {
            ignoreList = Interface.Oxide.DataFileSystem.ReadObject<IgnoreList>(Name);
            timer.Every((float)((double)Config["RemindTime(mins)"] * 60f), () => {
                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) {
                    if (ignoreList.players.Contains(BasePlayer.activePlayerList[i].userID)) continue;
                    BasePlayer.activePlayerList[i].ChatMessage("<size=20><color=#42c5f5>Make sure to join our Discord server by doing /discord</color></size>\n<color=#242424>Type /discord ignore to hide this reminder.</color>");
                }
            });
        }

        [ChatCommand("discord")]
        private void DiscordCommand(BasePlayer player, string command, string[] args) {
            if (args.Length == 1 && args[0].Equals("ignore")) {
                //add player to ignore list
                ignoreList.players.Add(player.userID);
                Interface.Oxide.DataFileSystem.WriteObject(Name, ignoreList);
                return;
            }

            string message =
                "<size=20><color=#0054ff>J</color><color=#002fff>o</color><color=#000aff>i</color><color=#1a00ff>n</color><color=#3f00ff> </color><color=#6400ff>O</color><color=#8a00ff>u</color><color=#af00ff>r</color><color=#d400ff> </color><color=#f900ff>D</color><color=#ff00df>i</color><color=#ff00b9>s</color><color=#ff0094>c</color><color=#ff006f>o</color><color=#ff004a>r</color><color=#ff0025>d</color></size>\n" +
                "To view the discord invite open your console by pressing F1.";
            player.ChatMessage(message);
            player.SendConsoleCommand("echo <size=40><color=#0054ff>J</color><color=#0033ff>o</color><color=#0012ff>i</color><color=#0e00ff>n</color><color=#2f00ff> </color><color=#5000ff>O</color><color=#7100ff>u</color><color=#9200ff>r</color><color=#b300ff> </color><color=#d400ff>D</color><color=#f500ff>i</color><color=#ff00e7>s</color><color=#ff00c6>c</color><color=#ff00a5>o</color><color=#ff0084>r</color><color=#ff0063>d</color><color=#ff0042> </color><color=#ff0021>@</color></size>\n<size=30><color=#42c5f5>Discord.gg/khkZKpCXjY</color></size>");
            global::ConsoleNetwork.BroadcastToAllClients("chat.add2", new object[] {
                    0,
                    player.userID,
                    "khkZKpCXjY",
                    "Bismuth Discord Code",
                    "#f00",
                    1f
            });
        }

        protected override void LoadDefaultConfig() {
            Puts("Creating a new configuration file.");
            Config["RemindTime(mins)"] = 30f;
        }
    }
}
