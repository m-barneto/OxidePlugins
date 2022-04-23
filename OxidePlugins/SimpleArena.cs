
using Rust;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("SimpleArena", "Mattdokn", 1.0)]
    class SimpleArena : RustPlugin {
        class Data {
            
        }
        class Arena {
        }
        class Teleporter : MonoBehaviour {
            public Vector3 dest = null;

            SphereCollider sphereCollider = null;

            private void Awake() {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "Teleporter";
            }
            public void Init(Vector3 pos, float radius, string id) {
                if (sphereCollider == null) {
                    sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.isTrigger = true;
                }
                sphereCollider.radius = radius;
                this.transform.position = pos;
            }

            public void SetDest(Vector3 dest) {
                this.dest = dest;
            }

            private void OnTriggerEnter(Collider col) {
                if (dest == null) return;

                BaseEntity baseEntity = col?.ToBaseEntity();
                if (!baseEntity?.IsValid() ?? false)
                    return;

                if (baseEntity is BasePlayer) {
                    BasePlayer player = (BasePlayer)baseEntity;
                    //Yo this mf just entered the collider
                }
            }
        }

        void Loaded() {

        }

        void Unload() {

        }

        #region Arena Setup
        [ChatCommand("")]
        private void PlaceTeleporter(BasePlayer player, string command, string[] args) {
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin)) return;

        }
        private void PlaceReceiver(BasePlayer player, string command, string[] args) {
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin)) return;

        }
        private void ListTeleporters(BasePlayer player, string command, string[] args) {
            if (!player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin)) return;

        }
        #endregion
    }
}
