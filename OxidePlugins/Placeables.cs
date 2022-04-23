using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Placeables", "Mattdokn", 1.0)]
    class Placeables : RustPlugin {
        /**
         * Use a data file to store netids of baseentities that are stacked, maybe use a one way linked list starting at the top and going down
         * for the bottom one execute a timer and if its not alive next frame then break the stacked boxes as well
         */
        class DataContainer {
            public Dictionary<uint, uint> stackables = new Dictionary<uint, uint>();
        }
        DataContainer data;
        void Loaded() {
            data = Interface.Oxide.DataFileSystem.ReadObject<DataContainer>(Name);
        }
        object OnServerMessage(string message, string name, string color, ulong id) {
            Puts($"{message}");
            return null;
        }
        object OnEntityGroundMissing(BaseEntity entity) {
            if (data.stackables.ContainsKey(entity.net.ID)) {
                return true;
            }
            return null;
        }
        void OnPlayerInput(BasePlayer player, InputState input) {
            Item activeItem = player.GetActiveItem();
            if (activeItem == null || !input.WasJustPressed(BUTTON.FIRE_PRIMARY)) return;
            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 5f)) return;
            BaseEntity hitEnt = hit.GetEntity();
            if (data.stackables.ContainsKey(hitEnt.net.ID)) {
                if (data.stackables[hitEnt.net.ID] != 0) return;
                Puts("old mcdonald had a farm");
                if (hitEnt.PrefabName.Equals("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab")) {
                    data.stackables[hitEnt.net.ID] = HandleSpawn(player, hitEnt, 0.75f);
                } else if (hitEnt.PrefabName.Equals("assets/prefabs/misc/halloween/coffin/coffinstorage.prefab")) {
                    data.stackables[hitEnt.net.ID] = HandleSpawn(player, hitEnt, 0.75f);
                } else if (hitEnt.PrefabName.Equals("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab")) {
                    data.stackables[hitEnt.net.ID] = HandleSpawn(player, hitEnt, 0.75f);
                }
                Interface.Oxide.DataFileSystem.WriteObject(Name, data);
            } else {
                Puts("EIEIOOOO");
                if (hitEnt.PrefabName.Equals("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab")) {
                    data.stackables[hitEnt.net.ID] = 0;
                } else if (hitEnt.PrefabName.Equals("assets/prefabs/misc/halloween/coffin/coffinstorage.prefab")) {
                    data.stackables[hitEnt.net.ID] = 0;
                } else if (hitEnt.PrefabName.Equals("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab")) {
                    data.stackables[hitEnt.net.ID] = 0;
                }
                Interface.Oxide.DataFileSystem.WriteObject(Name, data);
            }
        }
        int count = 0;
        void OnEntityKill(BaseCombatEntity entity) {
            if (!(entity is StorageContainer)) return;
            if (data.stackables.ContainsKey(entity.net.ID)) {
                Puts($"{count}");
                count++;
                if (data.stackables[entity.net.ID] != 0) {
                    (BaseNetworkable.serverEntities.Find(data.stackables[entity.net.ID]) as BaseCombatEntity).Die();
                }
                data.stackables.Remove(entity.net.ID);
                Interface.Oxide.DataFileSystem.WriteObject(Name, data);
            }
        }
        uint HandleSpawn(BasePlayer player, BaseEntity ent, float offset) {
            Puts($"Handled spawn of {ent.PrefabName}");
            List<Item> collect = new List<Item>();
            player.inventory.Take(collect, player.GetActiveItem().info.itemid, 1);
            Puts($"Just took {collect[0].info.shortname}");
            BaseEntity chest = GameManager.server.CreateEntity(ent.PrefabName, ent.transform.position + new Vector3(0f, offset, 0f), ent.transform.rotation);
            chest.Spawn();
            data.stackables[chest.net.ID] = 0;
            return chest.net.ID;
        }
    }
}
