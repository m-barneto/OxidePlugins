
using System.Collections.Generic;

namespace Oxide.Plugins {
    [Info("InstantLoot", "Mattdokn", 1.0)]
    class InstantLoot : RustPlugin {
        object OnContainerDropItems(ItemContainer container) {
            List<BasePlayer> players = new List<BasePlayer>();
            Vis.Entities(container.dropPosition, 4f, players);
            if (players.Count <= 0) return null;
            for (int i = 0; i < container.itemList.Count; i++) {
                players[0].GiveItem(container.itemList[i]);
            }
            return false;
        }
    }
}
