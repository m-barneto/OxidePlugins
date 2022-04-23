
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Aimbotter", "Mattdokn", 1.0)]
    class Aimbotter : RustPlugin {
        // Network Group lookup dictionary
        // Key is the user, Value is a list of all players that are networked to the key player
        private Dictionary<uint, List<uint>> players = new Dictionary<uint, List<uint>>();
        // Monobehaviour used to simulate custom bullet trajectory
        ProjectileSimulator projectileSimulator = null;
        // Each bullet has a different velocity
        Dictionary<string, int> ammoVelocities = new Dictionary<string, int>() {
            {"ammo.rifle", 375},
            {"ammo.rifle.hv", 450},
            {"ammo.rifle.incendiary", 225},
            {"ammo.rifle.explosive", 225},
            {"ammo.pistol", 300},
            {"ammo.pistol.hv", 400},
            {"ammo.pistol.fire", 225},
            {"ammo.shotgun.slug", 225}
        };
        void OnServerInitialized() {
            // Create a gameobject and add our projectile sim. script to it
            var go = new GameObject();
            go.name = "projectilesimulator";
            projectileSimulator = go.AddComponent<ProjectileSimulator>();
            projectileSimulator.Init();

            // If the plugin was reloaded, we need to setup our networked dict again
            foreach (var player in BasePlayer.activePlayerList) {
                players.Add(player.net.ID, new List<uint>());
            }
        }
        void Unload() {
            // Destroy our projectile simulator
            GameObject.Destroy(projectileSimulator);
        }

        // Get closest entity to players crosshair, within a fov
        BasePlayer GetClosestEntity(BasePlayer player, float fov = -.97f) {
            uint bestEnt = 0;
            float bestDist = float.MaxValue;
            // Go through each entity in the players networked player dict
            foreach (var entID in players[player.net.ID]) {
                // Get the ent as a baseplayer
                BasePlayer ent = BaseNetworkable.serverEntities.entityList[entID] as BasePlayer;
                // Get the direction from our player, to the target
                var dir = (player.eyes.position - ent.eyes.position).normalized;
                // Get the dot product of our players facing direction, and the direction to the entity
                var dot = Vector3.Dot(dir, player.eyes.BodyForward().normalized);
                // If the target entity is closest to the center, replace our current best target entity
                if (dot < bestDist && dot < fov) { // fov check goes here
                    bestDist = dot;
                    bestEnt = entID;
                }
            }
            // If no target entities were found in the players networked dict, then bestEnt will be left at 0, return null
            // Otherwise, return the best target entity as a baseplayer
            return bestEnt == 0 ? null : BaseNetworkable.serverEntities.entityList[bestEnt] as BasePlayer;
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles) {
            string ammoType = projectile.primaryMagazine.ammoType.shortname;
            // If the bullet fired isn't in our velocity lookup dict, return to default behaviour
            if (!ammoVelocities.ContainsKey(ammoType)) return;
            // Get the closest ent to the players crosshair
            BasePlayer target = GetClosestEntity(player);
            // If we have a valid target
            if (target != null) {
                // Shoot them
                projectileSimulator.FireProjectile(projectile.GetItem(), mod, player, player.eyes.position, (target.eyes.position - player.eyes.position).normalized * ammoVelocities[ammoType]);
            }
        }
        // On player connect, add them as a key in our networked player dict
        void OnPlayerConnected(BasePlayer player) => players.Add(player.net.ID, new List<uint>());
        // On player disconnect, remove them from our networked dict, and remove them from every other player's networked list
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            players.Remove(player.net.ID);
            foreach (var p in players) {
                p.Value.Remove(player.net.ID);
            }
        }
        // IMPORTANT |
        // IMPORTANT |
        // IMPORTANT |
        // IMPORTANT V
        // NOTE if a player teleports using some other plugin, our dict needs to have their list cleared
        // On user respawn, reset their list
        void OnUserRespawn(BasePlayer player) => players.Add(player.net.ID, new List<uint>());
        // When a player dies, remove them from every other player's networked list
        void OnPlayerDeath(BasePlayer player, HitInfo info) {
            players.Remove(player.net.ID);
            foreach (var p in players) {
                p.Value.Remove(player.net.ID);
            }
        }
        // When moving around, when the player loads new entities in, this is triggered
        void OnNetworkGroupEntered(BasePlayer player, Network.Visibility.Group group) {
            // NetworkGroupEntered happens before OnPlayerConnected, so you need to instantiate the players networked list here as well as on connected
            if (!players.ContainsKey(player.net.ID)) players.Add(player.net.ID, new List<uint>());
            // Iterate through all the entities that were networked to our player
            IEnumerator<Network.Networkable> iter = group.networkables.GetEnumerator();
            while (iter.MoveNext()) {
                BasePlayer oPlayer = BaseNetworkable.serverEntities.entityList[iter.Current.ID] as BasePlayer;
                // If the entity is a baseplayer
                // And the player is a player.prefab (not a scientist) and isn't an NPC
                // NOTE, I used spawned in fakeplayers to test aimbot functionality, removing the IsNpc check allowed me to do this.
                if (oPlayer != null && oPlayer.PrefabName.Equals("assets/prefabs/player/player.prefab") && !oPlayer.IsNpc) { // && !oPlayer.IsNpc
                    players[player.net.ID].Add(iter.Current.ID);
                }
            }
        }
        // When a player moves away from entities, unload them
        void OnNetworkGroupLeft(BasePlayer player, Network.Visibility.Group group) {
            // Iterate through all the entities that were unloaded from our player
            IEnumerator<Network.Networkable> iter = group.networkables.GetEnumerator();
            while (iter.MoveNext()) {
                BasePlayer oPlayer = BaseNetworkable.serverEntities.entityList[iter.Current.ID] as BasePlayer;
                // If the entity is a baseplayer
                // And the player is a player.prefab (not a scientist) and isn't an NPC
                // NOTE, I used spawned in fakeplayers to test aimbot functionality, removing the IsNpc check allowed me to do this.
                if (oPlayer != null && oPlayer.PrefabName.Equals("assets/prefabs/player/player.prefab") && !oPlayer.IsNpc) { // && !oPlayer.IsNpc
                    players[player.net.ID].Remove(iter.Current.ID);
                }
            }
        }

        class ProjectileSimulator : MonoBehaviour {
            List<BasePlayer.FiredProjectile> firedProjectiles = new List<BasePlayer.FiredProjectile>();
            Effect headshotEffect = new Effect("assets/bundled/prefabs/fx/headshot.prefab", default(Vector3), default(Vector3));
            Effect hitNotifyEffect = new Effect("assets/bundled/prefabs/fx/hit_notify.prefab", default(Vector3), default(Vector3));

            public void Init() {
                InvokeRepeating(nameof(Simulate), 0.03125f, 0.03125f);
            }

            public void Add(BasePlayer.FiredProjectile firedProjectile) {
                firedProjectiles.Add(firedProjectile);
            }

            void Simulate() {
                // Iterate backwards so we can remove them without issue
                for (int i = firedProjectiles.Count - 1; i >= 0; i--) {
                    var firedProjectile = firedProjectiles.ElementAt(i);
                    bool status = SimulateProjectile(firedProjectile, 0.03125f);

                    if (!status) {
                        firedProjectiles.RemoveAt(i);
                    }
                }
            }

            // If this method returns false, the bullet is deleted from the firedProjectiles list
            bool SimulateProjectile(BasePlayer.FiredProjectile firedProjectile, float dt) {
                #region Some magic stuff, just know it simulates the bullets trajectory
                if (firedProjectile.travelTime == 0) {
                    dt = Time.realtimeSinceStartup - firedProjectile.firedTime;
                } else if (firedProjectile.travelTime > 8.0f) {
                    // If the bullet has been travelling for over 8 seconds, remove it
                    return false;
                }

                Vector3 gravity = UnityEngine.Physics.gravity * firedProjectile.projectilePrefab.gravityModifier;
                Vector3 oldPosition = firedProjectile.position;
                firedProjectile.position += firedProjectile.velocity * dt;
                firedProjectile.velocity += gravity * dt;
                firedProjectile.velocity -= firedProjectile.velocity * firedProjectile.projectilePrefab.drag * dt;
                firedProjectile.travelTime += dt;
                #endregion

                Vector3 vec = firedProjectile.position - oldPosition;
                var list = new List<RaycastHit>();
                GamePhysics.TraceAll(new Ray(oldPosition, vec), 0f, list, vec.magnitude, 1219701521, QueryTriggerInteraction.Ignore);

                bool didntHit = true;

                // Loop through all the entities hit by our bullet (usually just one entity)
                foreach (var hit in list) {
                    BaseEntity ent = hit.GetEntity();
                    BasePlayer player = BaseNetworkable.serverEntities.entityList[(uint)firedProjectile.hits] as BasePlayer;
                    didntHit = false;
                    HitInfo hitInfo = new HitInfo();
                    hitInfo.ProjectilePrefab = firedProjectile.projectilePrefab;
                    hitInfo.Initiator = player;
                    hitInfo.ProjectileIntegrity = 1f;
                    hitInfo.Weapon = firedProjectile.weaponSource;
                    hitInfo.WeaponPrefab = firedProjectile.weaponPrefab;
                    hitInfo.HitEntity = ent;
                    hitInfo.HitPositionWorld = hit.point;
                    hitInfo.HitNormalWorld = hit.normal;
                    hitInfo.PointStart = firedProjectile.initialPosition;
                    hitInfo.ProjectileDistance = (firedProjectile.position - firedProjectile.initialPosition).magnitude;
                    hitInfo.ProjectilePrefab.CalculateDamage(hitInfo, firedProjectile.projectilePrefab.modifier, 1);
                    firedProjectile.itemMod.ServerProjectileHit(hitInfo);
                    Effect.server.ImpactEffect(hitInfo);
                    if (ent == null) {
                        return false;
                    }

                    BasePlayer target = ent as BasePlayer;
                    if (target != null) {
                        EffectNetwork.Send(hitNotifyEffect, player.Connection);
                        target.Hurt(hitInfo);
                    }


                    if (ent.ShouldBlockProjectiles()) {
                        break;
                    }
                }

                return didntHit;
            }

            public void FireProjectile(Item item, ItemModProjectile mod, BasePlayer player, Vector3 position, Vector3 velocity) {

                //var item = ItemManager.CreateByName("ammo.rifle.incendiary");
                var projectile = mod.projectileObject.Get().GetComponent<Projectile>();

                //Puts($"itemModProjectile.projectileVelocity: {itemModProjectile.projectileVelocity}");

                BasePlayer.FiredProjectile firedProjectile = new BasePlayer.FiredProjectile {
                    hits = (int)player.net.ID,
                    weaponSource = projectile.sourceWeaponPrefab,
                    weaponPrefab = projectile.sourceWeaponPrefab,
                    itemDef = item.info,
                    itemMod = mod,
                    projectilePrefab = projectile,
                    firedTime = UnityEngine.Time.realtimeSinceStartup,
                    travelTime = 0f,
                    position = position,
                    velocity = velocity,
                    initialPosition = position,
                    initialVelocity = velocity
                };

                Add(firedProjectile);

                Effect effect = new Effect();
                effect.Clear();
                effect.Init(global::Effect.Type.Projectile, position, velocity, null);
                effect.scale = 2;
                effect.pooledString = mod.projectileObject.resourcePath;
                effect.number = 1;
                EffectNetwork.Send(effect);
            }
        }
    }

}
