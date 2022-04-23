using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Plugins {
    [Info("ClanPermissions", "Mattdokn", 1.0)]
    [Description("Share certain permissions across fellow clan members")]
    class ClanPermissions : RustPlugin {
        [PluginReference("Clans")]
        private Plugin clans = null;

        const string GROUP_PREFIX = "clanpermissions.";

        private void Loaded() {
            if (clans == null) {
                PrintError("Clans is not loaded, get it at https://umod.org/plugins/clans");
                SendCmd("o.unload ClanPermissions");
                return;
            }
        }

        private void SendCmd(string command) {
            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "o.unload ClanPermissions");
        }

        [ChatCommand("cgroup")]
        private void ClanGroupPerms(BasePlayer player, string command, string[] args) {
            string[] perms = permission.GetGroupPermissions(GROUP_PREFIX + clans.Call<string>("GetClanOf", player.userID));
            OnClanUpdate(clans.Call<string>("GetClanOf", player.userID));
            StringBuilder builder = new StringBuilder("You have access to the following shared permissions: \n");
            foreach (string p in perms) builder.AppendLine("    " + p);
            player.ChatMessage(builder.ToString());
        }

        private void OnClanCreate(string tag) {
            Puts("Created clan: " + tag);
            permission.CreateGroup(GROUP_PREFIX + tag, tag, 1);
        }
        private void OnClanDestroy(string tag) {
            Puts("Disbanded clan: " + tag);
            permission.RemoveGroup(GROUP_PREFIX + tag);
        }

        private void OnClanUpdate(string tag) {
            Puts("Updated clan");
            JObject clan = clans.Call<JObject>("GetClan", tag);
            JArray members = (JArray)clan["members"];
            for (int i = 0; i < members.Count; i++) {
                Puts("Added member " + members[i].ToString() + " to group " + tag);
                permission.AddUserGroup(members[i].ToString(), GROUP_PREFIX + tag);
                foreach (string perm in Config.Get<List<string>>("SharedPermissions")) {
                    Puts("Checking perm: " + perm);
                    if (permission.UserHasPermission(members[i].ToString(), perm)) {
                        Puts("User had perm: " + perm);
                        if (!permission.GroupHasPermission(GROUP_PREFIX + tag, perm)) {
                            Puts("Added " + perm + " to group.");
                            permission.GrantGroupPermission(GROUP_PREFIX + tag, perm, this);
                            permission.SaveData();
                        }
                    }
                }
            }
        }

        // Default Config
        protected override void LoadDefaultConfig() {
            Config.Clear();
            List<string> sharedPermissions = new List<string> {
                "basic.permission"
            };
            Config["SharedPermissions"] = sharedPermissions;
        }
    }
}
