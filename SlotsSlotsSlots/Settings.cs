using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotsSlotsSlots
{
    public record Settings
    {
        public float BaseCarryWeight = 25.0f;
        public float EffectMultiplyer = 0.2f;
        public float PotionWeights = 0.1f;
        public bool NoHealFromWeightless = true;
    }
}
