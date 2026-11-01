using System;
using System.Collections.Generic;
using WarInLocked.Core.Characters;

namespace WarInLocked.Core.Items
{
    /// <summary>One thing a shop sells and what it costs (copper). Stock is unlimited.</summary>
    public sealed class ShopEntry
    {
        public ShopEntry(Item item, int priceCopper)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            if (priceCopper < 0) throw new ArgumentOutOfRangeException(nameof(priceCopper));
            PriceCopper = priceCopper;
        }

        public Item Item { get; }
        public int PriceCopper { get; }
        /// <summary>The price ready to print, e.g. "25c" (see <see cref="Money.Format"/>).</summary>
        public string PriceText => Money.Format(PriceCopper);
    }

    /// <summary>What happened when the player tried to buy (#28). Nothing changes when it did not succeed.</summary>
    public sealed class BuyResult
    {
        private BuyResult(bool success, string reason, ShopEntry entry, IReadOnlyList<WarInLocked.Core.Quests.Quest> ready)
        {
            Success = success;
            Reason = reason;
            Entry = entry;
            ReadyQuests = ready ?? new WarInLocked.Core.Quests.Quest[0];
        }

        public static BuyResult Bought(ShopEntry entry, IReadOnlyList<WarInLocked.Core.Quests.Quest> ready) => new BuyResult(true, null, entry, ready);
        public static BuyResult Refused(ShopEntry entry, string reason) => new BuyResult(false, reason, entry, null);

        public bool Success { get; }
        /// <summary>"Not enough copper", "Inventory full" or "Not for sale"; null on success.</summary>
        public string Reason { get; }
        public ShopEntry Entry { get; }
        /// <summary>Quests that became ready to turn in because of this purchase (e.g. "Shopping before adventure").</summary>
        public IReadOnlyList<WarInLocked.Core.Quests.Quest> ReadyQuests { get; }
    }

    /// <summary>
    /// A shop (#28), the data behind the shop screen: a Buy tab (works) and a Sell tab (disabled for now); selecting an item offers Buy
    /// and Haggle (disabled for now). The screen only draws what this exposes and calls <see cref="Buy"/>.
    /// </summary>
    public sealed class Shop
    {
        public const string NotEnoughCopperText = "Not enough copper";
        public const string InventoryFullText = "Inventory full";
        public const string NotForSaleText = "Not for sale";

        public const string BuyTabText = "Buy";
        public const string SellTabText = "Sell";
        public const string BuyActionText = "Buy";
        public const string HaggleActionText = "Haggle";

        private readonly List<ShopEntry> _stock = new List<ShopEntry>();

        public Shop(string id, string name, IEnumerable<ShopEntry> stock)
        {
            Id = id;
            Name = name;
            if (stock != null) _stock.AddRange(stock);
        }

        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<ShopEntry> Stock => _stock;

        /// <summary>The Buy tab works.</summary>
        public bool BuyEnabled => true;
        /// <summary>The Sell tab is shown but disabled for now.</summary>
        public bool SellEnabled => false;
        /// <summary>The Haggle action is shown but disabled for now.</summary>
        public bool HaggleEnabled => false;

        public ShopEntry Find(string itemId) => _stock.Find(e => e.Item.Id == itemId);

        /// <summary>Price in copper, or -1 if this shop does not sell it.</summary>
        public int PriceOf(string itemId) => Find(itemId)?.PriceCopper ?? -1;

        /// <summary>Why the character cannot buy this item right now ("Not enough copper", "Inventory full"), or null if they can.</summary>
        public string CanBuy(Character character, string itemId)
        {
            var entry = Find(itemId);
            if (entry == null) return NotForSaleText;
            if (!character.CanAfford(entry.PriceCopper)) return NotEnoughCopperText;
            if (!character.Inventory.CanAdd(entry.Item, 1)) return InventoryFullText;
            return null;
        }

        /// <summary>
        /// Buys one. If <see cref="CanBuy"/> gives a reason nothing changes. Otherwise it spends the copper, adds the item and counts toward
        /// quests that want that purchase.
        /// </summary>
        public BuyResult Buy(Character character, string itemId)
        {
            string reason = CanBuy(character, itemId);
            var entry = Find(itemId);
            if (reason != null) return BuyResult.Refused(entry, reason);
            character.SpendCopper(entry.PriceCopper);
            character.Inventory.Add(entry.Item, 1);
            var ready = character.Quests.RecordPurchase(entry.Item.Id);
            return BuyResult.Bought(entry, ready);
        }
    }

    /// <summary>Known shops.</summary>
    public static class ShopLibrary
    {
        public const string ColonelPowersShopId = "colonel-powers-shop";
        /// <summary>Price of a Potion of lesser healing at the Colonel's shop (#28): 25 copper.</summary>
        public const int LesserHealingPotionPrice = 25;

        public static Shop ColonelPowers() => new Shop(ColonelPowersShopId, "Colonel Powers",
            new[] { new ShopEntry(ItemLibrary.LesserHealingPotion, LesserHealingPotionPrice) });

        /// <summary>A fresh shop with this id, or null.</summary>
        public static Shop Get(string id) => id == ColonelPowersShopId ? ColonelPowers() : null;
    }
}
