
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins {
    [Info("HealthySpawn", "Mattdokn", 1.0)]
    class HealthySpawn : RustPlugin {
        [PluginReference("ZoneManager")]
        private Plugin zoneManager = null;

        private const string healthZoneId = "";

        void Loaded() {
            if (zoneManager == null) {
                PrintError("Zone Manager is not loaded, get it at https://umod.org/plugins/zone-manager");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "o.unload HealthySpawn");
                return;
            }
        }

        void OnUserRespawned(IPlayer iPlayer) {
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(iPlayer.Id));
            if (player != null && zoneManager != null) {
                if (zoneManager.Call<bool>("IsPlayerInZone", healthZoneId)) {
                    player.health = 100f;
                    //player.metabolism.pending_health.Increase(100f);
                }
            }
        }
    }
}
