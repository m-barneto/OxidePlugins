
using System;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Plugins {
    [Info("BuyableHeli", "Mattdokn", 1.0)]
    class BuyableHeli : RustPlugin {
        public struct PriceInfo {
            public int amount;
            public string displayName;
        }

        private Dictionary<ulong, long> playerCooldowns = new Dictionary<ulong, long>();

        [ChatCommand("buyheli")]
        private void BuyHeli(BasePlayer player, string command, string[] args) {
            if (playerCooldowns.ContainsKey(player.userID)) {
                long timeDiff = DateTime.Now.Ticks - DateTime.FromBinary(playerCooldowns[player.userID]).Ticks;
                TimeSpan span = new TimeSpan(timeDiff);
                if (span.TotalSeconds >= (int)Config["Cooldown"]) playerCooldowns.Remove(player.userID);
                else {
                    string msg = $"<color=#ff6b7a>[BuyableHeli]</color>: Please wait {span.TotalMinutes} minutes before calling another <color=#ff6b7a>patrol helicopter</color>.";
                    player.ChatMessage(msg);
                    return;
                }
            }

            Dictionary<string, int> missingItems = new Dictionary<string, int>();
            foreach (var item in Config.Get<Dictionary<string, int>>("Price")) {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(item.Key);
                if (itemDef != null) {
                    int missingAmount = item.Value - player.inventory.GetAmount(itemDef.itemid);
                    if (missingAmount > 0) {
                        missingItems.Add(item.Key, missingAmount);
                    }
                }
            }
            if (missingItems.Count > 0) {
                StringBuilder msgBuilder = new StringBuilder("<color=#ff6b7a>[BuyableHeli]</color>: You don't have enough resources to buy a <color=#ff6b7a>patrol helicopter</color>.\nYou're missing:");
                foreach (var res in missingItems) {
                    msgBuilder.Append($"\n <color=#ff6b7a>>{res.Key}</color> x<color=#707fff>{res.Value}</color>");
                }
                player.ChatMessage(msgBuilder.ToString());
            } else {
                //call the heli and take items
                List<Item> collect = new List<Item>();
                foreach (var item in Config.Get<Dictionary<string, int>>("Price")) {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(item.Key);
                    player.inventory.Take(collect, itemDef.itemid, item.Value);
                    player.Command("note.inv", itemDef.itemid, -item.Value);
                }
                foreach (var item in collect) item.Remove();
                collect.Clear();
                player.ChatMessage("<color=#ff6b7a>[BuyableHeli]</color>: <color=#ff6b7a>Patrol helicopter</color> inbound!");
                playerCooldowns.Add(player.userID, DateTime.Now.Ticks);
                CallHeli(player);
            }
        }

        private void CallHeli(BasePlayer player) {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin)) {
                Puts("Hello");
                player.SendConsoleCommand("heli.calltome");
            } else {
                Puts("EEEEE");
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
                player.SendConsoleCommand("heli.calltome");
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }
        }

        protected override void LoadDefaultConfig() {
            Config["Cooldown"] = 1800;
            Dictionary<string, int> items = new Dictionary<string, int>();
            items.Add("Scrap", 8000);
            Config["Price"] = items;
        }
    }
}
