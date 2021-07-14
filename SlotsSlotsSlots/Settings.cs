using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.Synthesis.Settings;

namespace SlotsSlotsSlots
{
    public record Settings
    {   
        [SynthesisOrder]
        [SynthesisSettingName("Race Carry Weight Multiplier")]
        [SynthesisDescription("This multiplies only the base value, the stat will still be upgraded on levelup, and with perks/spells/enchantments.")]
        [SynthesisTooltip("This multiplies only the base value,the stat will still be upgraded on levelup, and with perks/spells/enchantments.")]
        public float BaseMultiplier = 0.1f;
        [SynthesisOrder]
        [SynthesisSettingName("Carry Weight Effect Multiplier")]
        [SynthesisDescription("This is a value all loaded effects that alter carryweight get multiplied with to make them work with the lower weigth values of the slot system.")]
        [SynthesisTooltip("This is a value all loaded effects that alter carryweight get multiplied with to make them work with the lower weigth values of the slot system.\nA value of 0.2 would mean the effect has 20% effectiveness.")]
        public float CarryweightEffectMultiplier = 0.2f;
        [SynthesisOrder]
        [SynthesisSettingName("Potion Weight")]
        [SynthesisDescription("This alters the weigth of any potion in the game, to set much slots 1 potion should take up.")]
        [SynthesisTooltip("This alters the weigth of any potion in the game, to set much slots 1 potion should take up.\nA value of 0.1 makes it, so 10 potions are needed to fill 1 slot.")]
        public float PotionSlotUse = 0.1f;
        [SynthesisOrder]
        [SynthesisSettingName("Scroll Weight")]
        [SynthesisDescription("This alters the weigth of any scroll in the game, to set much slots 1 scroll should take up.")]
        [SynthesisTooltip("This alters the weigth of any scroll in the game, to set much slots 1 scroll should take up.\nA value of 0.5 makes it, so 2 scrolls are needed to fill 1 slot.")]
        public float ScrollSlotUse = 0.5f;
        [SynthesisOrder]
        [SynthesisSettingName("Weigthless items can't heal")]
        [SynthesisDescription("This disables the healing effect from any item that isn't a potion, as they are excluded from the slot system.")]
        [SynthesisTooltip("This disables the healing effect from any item that isn't a potion, as they are excluded from the slot system.")]
        public bool WeightlessItemsOfferNoHealing = true;

        [SynthesisOrder]
        [SynthesisSettingName("Minimum Weaponslots")]
        [SynthesisDescription("This is the number of slots the lightest weapons will need.")]
        [SynthesisTooltip("This is the number of slots the lightest weapons will need.")]
        public int MinimumUsedWeaponSlots = 1;
        [SynthesisOrder]
        [SynthesisSettingName("Maximum Weaponslots")]
        [SynthesisDescription("This is the number of slots the heaviest weapons will need.")]
        [SynthesisTooltip("This is the number of slots the heaviest weapons will need.")]
        public int MaximumUsedWeaponSlots = 3;
        [SynthesisSettingName("Minimum Clothingslots")]
        [SynthesisDescription("This is the number of slots the lightest clothing will need.\nThis includes Jewelry and Shields.")]
        [SynthesisTooltip("This is the number of slots the lightest clothing will need.\nThis includes Jewelry and Shields.")]
        public int MinimumUsedArmorSlots = 1;
        [SynthesisSettingName("Maximum Clothingslots")]
        [SynthesisDescription("This is the number of slots the heaviest clothing will need.\nThis includes Jewelry and Shields.")]
        [SynthesisTooltip("This is the number of slots the heaviest clothing will need.\nThis includes Jewelry and Shields.")]
        public int MaximumUsedArmorSlots = 6;
    }
}
