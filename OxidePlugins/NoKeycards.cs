
namespace Oxide.Plugins {
    [Info("NoKeycards", "Mattdokn", 1.0)]
    class NoKeycards : RustPlugin {
        void OnEntitySpawned(BaseNetworkable entity) {
            if (entity.PrefabName.Contains("keycard.entity.prefab")) {
                entity.Kill();
            }
        }
    }
}
