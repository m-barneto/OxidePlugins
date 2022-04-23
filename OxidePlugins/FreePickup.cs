
namespace Oxide.Plugins {
    [Info("FreePickup", "Mattdokn", 1.0)]
    class FreePickup : RustPlugin {
        bool CanPickupEntity(BasePlayer player, BaseEntity entity) {
            if (entity is BaseCombatEntity) {
                BaseCombatEntity ent = (BaseCombatEntity)entity;
                ent.pickup.subtractCondition = 0f;
            }
            return true;
        }
    }
}
