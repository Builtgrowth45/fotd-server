using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Core.Items;
using FOMServer.Shared.Interop.FOMNetwork.Constants;
using FOMServer.Shared.Interop.FOMNetwork.Enums.Item;
using FOMServer.World.Core.Players;
using FOMServer.World.Tests.Factories;

namespace FOMServer.World.Tests.Players
{
    public class PlayerEquipmentTests
    {
        private const ItemSlotType WeaponSlot = ItemSlotType.WeaponStart;

        [Fact]
        public void FireWeapon_SpendsAmmoAndRaisesEquipmentChanged()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 1000, durabilityLossFactor: 0, out var weapon);

            var changes = 0;
            player.Equipment.EquipmentChanged += (_, _) => ++changes;

            Assert.Equal(1, player.Equipment.FireWeapon(WeaponSlot));

            Assert.Equal(9, ValueOf(weapon));
            Assert.Equal(1, changes);
        }

        [Fact]
        public void FireWeapon_MultipleRounds_SpendsThatMany()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 1000, durabilityLossFactor: 0, out var weapon);

            Assert.Equal(4, player.Equipment.FireWeapon(WeaponSlot, 4));

            Assert.Equal(6, ValueOf(weapon));
        }

        [Fact]
        public void FireWeapon_MoreRoundsThanRemain_SpendsOnlyWhatIsLeft()
        {
            var player = BuildPlayerWithWeapon(value: 3, durability: 1000, durabilityLossFactor: 0, out var weapon);

            Assert.Equal(3, player.Equipment.FireWeapon(WeaponSlot, 10));

            Assert.Equal(0, ValueOf(weapon));
        }

        [Fact]
        public void FireWeapon_WhenEmpty_SpendsNothingAndRaisesNoChange()
        {
            var player = BuildPlayerWithWeapon(value: 0, durability: 1000, durabilityLossFactor: 0, out _);

            var changes = 0;
            player.Equipment.EquipmentChanged += (_, _) => ++changes;

            Assert.Equal(0, player.Equipment.FireWeapon(WeaponSlot));
            Assert.Equal(0, changes);
        }

        [Fact]
        public void FireWeapon_ZeroRounds_DoesNothing()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 1000, durabilityLossFactor: 0, out var weapon);

            Assert.Equal(0, player.Equipment.FireWeapon(WeaponSlot, 0));

            Assert.Equal(10, ValueOf(weapon));
        }

        [Fact]
        public void FireWeapon_WearsDownDurability()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 1000, durabilityLossFactor: 100, out var weapon);

            player.Equipment.FireWeapon(WeaponSlot, 5);

            Assert.Equal(995, DurabilityOf(weapon));
        }

        [Fact]
        public void FireWeapon_UntilDurabilityRunsOut_DeletesTheWeapon()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 3, durabilityLossFactor: 100, out var weapon);

            Assert.Equal(3, player.Equipment.FireWeapon(WeaponSlot, 3));

            Assert.True(weapon.IsBroken);
            Assert.True(weapon.IsDeleted);
        }

        [Fact]
        public void FireWeapon_EmptySlot_SpendsNothing()
        {
            var player = TestPlayerBuilder.Create().Build();

            Assert.Equal(0, player.Equipment.FireWeapon(WeaponSlot));
        }

        [Fact]
        public void FireWeapon_NonWeaponSlot_SpendsNothing()
        {
            var player = BuildPlayerWithWeapon(value: 10, durability: 1000, durabilityLossFactor: 0, out var weapon);

            Assert.Equal(0, player.Equipment.FireWeapon(ItemSlotType.EquipmentStart));

            Assert.Equal(10, ValueOf(weapon));
        }

        private static Player BuildPlayerWithWeapon(
            ushort value,
            ushort durability,
            byte durabilityLossFactor,
            out Item weapon
        )
        {
            weapon = new Item(
                id: 1,
                type: ItemType.GakkMG6,
                locationType: ItemLocationType.Equipment,
                locationId: 1,
                slot: WeaponSlot,
                value: value,
                valueMax: 100,
                durability: durability,
                durabilityLossFactor: durabilityLossFactor,
                security: default,
                rarity: default,
                creatorPlayerId: 0,
                stolenFromPlayerId: 0,
                timeout: 0,
                attributeBonus: 0,
                recipeVariation: 0,
                recipeBalanceValues: new byte[BufferSizes.NumItemBalanceSliders]
            );

            return TestPlayerBuilder.Create(1).WithItem(ItemContainerType.Equipment, weapon).Build();
        }

        private static int ValueOf(Item item)
        {
            return item.ToSnapshot().Value;
        }

        private static int DurabilityOf(Item item)
        {
            return item.ToSnapshot().Durability;
        }
    }
}
