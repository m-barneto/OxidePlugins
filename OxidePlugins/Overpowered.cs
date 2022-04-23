using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins {
    [Info("Overpowered", "Mattdokn", 1.0)]
    internal class Overpowered : RustPlugin {
        List<Type> elecComponents = new List<Type>() { };
        IEnumerable<IOEntity> ioEntityClasses = typeof(IOEntity)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(IOEntity)) && !t.IsAbstract)
            .Select(t => (IOEntity)Activator.CreateInstance(t));

        void Init() {
            foreach (var ioEntType in ioEntityClasses) {
                if (ioEntType.WantsPower()) {
                    elecComponents.Add(ioEntType);
                }
            }
        }
        void OnServerInitialized() {

        }
        void Unload() {
        }
        object OnHammerHit(BasePlayer player, HitInfo info) {
            if (info.HitEntity is BuildingBlock) {
                BuildingBlock bb = (BuildingBlock)info.HitEntity;
                BuildingManager.Building building = BuildingManager.server.GetBuilding(bb.buildingID);
                foreach (BaseCombatEntity ent in building.decayEntities) {
                    if (ent is IOEntity) {
                        IOEntity ioEnt = (IOEntity)ent;
                        ioEnt.SetFlag(BaseEntity.Flags.Reserved8, true);
                        Puts($"{ent.ShortPrefabName}");
                    }
                    if (ent is AutoTurret) {
                        AutoTurret turret = (AutoTurret)ent;
                        turret.InitiateStartup();
                        turret.SetFlag(BaseEntity.Flags.Reserved8, true);
                        turret.UpdateHasPower(11, 0);
                    }
                }
                Puts($"{player.displayName} hit building id {bb.buildingID}");
            }
            return null;
        }
    }
}
