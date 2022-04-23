
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Helipad", "Mattdokn", 0.1)]
    class Helipad : RustPlugin {
        public static Helipad instance = null;

        [PluginReference]
        private Plugin ZoneManager = null;

        private const string SPAWN_PERM = "helipad.spawn";
        private const string REMOVE_PERM = "helipad.remove";

        class HelipadSpawner {
            public string id;
            public Vector3 pos;
            public bool spawnTcopOrMini;
            public uint spawnedHeli;
            Timer spawnTimer;
            bool canSpawn;

            public HelipadSpawner(string id, Vector3 pos, bool spawnTcopOrMini) {
                this.id = id;
                this.pos = pos;
                this.spawnTcopOrMini = spawnTcopOrMini;
                spawnedHeli = 0;
                canSpawn = true;
                spawnTimer = instance.timer.Once(5f, () => this.SpawnHeli());
            }

            public void HeliExitedArea(uint heliID) {
                if (heliID != spawnedHeli) return;

                Timer timer = null;
                timer = instance.timer.Every((float)(double)instance.Config["Decay heli time (seconds)"], () => {
                    MiniCopter copter = (MiniCopter)BaseNetworkable.serverEntities.Find(heliID);
                    if (copter != null && !copter.AnyMounted()) {
                        spawnedHeli = 0;
                        copter.AdminKill();
                        timer.Destroy();
                    }
                });

                if (canSpawn) {
                    spawnTimer = instance.timer.Once((float)(double)instance.Config["Heli spawn delay (seconds)"], () => {
                        SpawnHeli();
                    });
                    canSpawn = false;
                    instance.timer.Once(1f, () => canSpawn = true);
                }                
            }

            public void HeliEnteredArea(uint heliID) {
                if (heliID != spawnedHeli) return;
                spawnTimer.Destroy();
            }

            public void SpawnHeli() {
                BaseEntity heli = null;
                if (spawnTcopOrMini) {
                    heli = GameManager.server.CreateEntity("assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab", pos);
                } else {
                    heli = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab", pos);
                }
                heli.Spawn();
                spawnedHeli = heli.net.ID;
                Interface.Oxide.DataFileSystem.WriteObject(instance.Name, instance.helipadStorage);

                ItemContainer fuelContainer = (heli as MiniCopter)?.GetFuelSystem()?.GetFuelContainer()?.inventory;
                if (fuelContainer == null) return;
                Item fuel = ItemManager.CreateByItemID(-946369541, (int)instance.Config["Amount of LGF to place in heli"]);
                fuel.MoveToContainer(fuelContainer);
            }
        }
        private class HelipadStorage {
            public Dictionary<string, HelipadSpawner> helipads = new Dictionary<string, HelipadSpawner>();
            public HelipadStorage() { }
        }
        private HelipadStorage helipadStorage;

        void Init() {
            if (!permission.PermissionExists(SPAWN_PERM)) {
                permission.RegisterPermission(SPAWN_PERM, this);
            }
            if (!permission.PermissionExists(REMOVE_PERM)) {
                permission.RegisterPermission(REMOVE_PERM, this);
            }
            instance = this;
            helipadStorage = Interface.Oxide.DataFileSystem.ReadObject<HelipadStorage>(Name);
        }

        /**
         * Allow for helipad remove command to remove the nearest pad
         * Give configurable zone radius for each helipad
         */
        [ChatCommand("helipad")]
        void HelipadCommand(BasePlayer player, string command, string[] args) {
            if (args.Length >= 1) {
                if (args[0].Equals("spawn")) {
                    if (!permission.UserHasPermission(player.UserIDString, SPAWN_PERM) && !player.IsAdmin) {
                        player.ChatMessage("You do not have permission to spawn helipads.");
                        return;
                    }
                    if (args.Length <= 1 || !args[0].ToLower().Equals("spawn")) {
                        // add second arg for zone radius, and show correct usage with optional arg
                        player.ChatMessage("Incorrect usage, use /helipad spawn (name=helipad) (radius=10) (buildable (T/F)=F) (spawn tcop(T) or mini(M)=T)");
                        return;
                    }

                    string name = "Helipad";
                    if (args.Length >= 2) {
                        name = args[1];
                    }

                    float zoneRadius = 10f;
                    if (args.Length >= 3) {
                        if (!float.TryParse(args[2], out zoneRadius)) {
                            zoneRadius = 10f;
                        }
                    }

                    bool buildInZone = false;
                    if (args.Length >= 4) {
                        if (args[3].ToLower().Equals("t")) {
                            buildInZone = true;
                        } else if (args[3].ToLower().Equals("f")) {
                            buildInZone = false;
                        } else {
                            buildInZone = false;
                        }
                    }

                    bool spawnTcop = true;
                    if (args.Length >= 5) {
                        if (args[4].ToLower().Equals("t")) {
                            spawnTcop = true;
                        } else if (args[4].ToLower().Equals("m")) {
                            spawnTcop = false;
                        } else {
                            spawnTcop = true;
                        }
                    }
                    string id = CreateZoneAtPos(player.transform.position, name, zoneRadius, buildInZone ? "false" : "true");
                    if (id == null) {
                        PrintError("Failed to create zone!");
                        return;
                    }

                    helipadStorage.helipads[id] = new HelipadSpawner(name, player.transform.position, spawnTcop);
                    Interface.Oxide.DataFileSystem.WriteObject(Name, helipadStorage);
                } else if (args[0].Equals("remove")) {
                    if (!permission.UserHasPermission(player.UserIDString, REMOVE_PERM) && !player.IsAdmin) {
                        player.ChatMessage("You do not have permission to remove helipads.");
                        return;
                    }

                    if (args.Length >= 2) {
                        if (ZoneManager.Call<bool>("EraseZone", args[1])) {
                            helipadStorage.helipads.Remove(args[1]);
                            player.ChatMessage($"{args[1]} was removed successfully.");
                        } else {
                            player.ChatMessage($"Failed to remove {args[1]}.");
                        }
                    } else {
                        player.ChatMessage("To list helipads, use /zone_list.");
                        player.ChatMessage("To remove a helipad use /helipad remove (name)");
                    }
                }
            } else {
                player.ChatMessage("Use /helipad remove/spawn to view command usage.");
            }
        }

        string CreateZoneAtPos(Vector3 pos, string zoneName, float zoneRadius, string buildInZone) {
            string name = UnityEngine.Random.Range(1000, 9999).ToString();

            while (ZoneManager.Call<string>("CheckZoneID", name) != null) {
                name = UnityEngine.Random.Range(1000, 9999).ToString();
            }
            if (!ZoneManager.Call<bool>("CreateOrUpdateZone", name, new string[] {"name", zoneName, "radius", zoneRadius.ToString(), "nobuild", buildInZone}, pos)) {
                return null;
            }
            return name;
        }

        void OnEntityExitZone(string ZoneID, BaseEntity entity) {
            if (!helipadStorage.helipads.ContainsKey(ZoneID)) return;
            helipadStorage.helipads[ZoneID].HeliExitedArea(entity.net.ID);
        }
        void OnEntityEnterZone(string ZoneID, BaseEntity entity) {
            if (!helipadStorage.helipads.ContainsKey(ZoneID)) return;
            helipadStorage.helipads[ZoneID].HeliEnteredArea(entity.net.ID);
        }

        protected override void LoadDefaultConfig() {
            Puts("Creating a new configuration file.");
            Config["Amount of LGF to place in heli"] = 1000;
            Config["Decay heli time (seconds)"] = 600.0f;
            Config["Heli spawn delay (seconds)"] = 180.0f;
        }
    }
}
