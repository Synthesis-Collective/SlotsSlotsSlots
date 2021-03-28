﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

namespace SlotsSlotsSlots
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SlotsSlotsSlots.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            float baseCarryWeight = 25.0f;
            float effectMultiplyer = 0.2f;
            float potionWeights = 0.1f;
            bool NoHealFromWeightless = true;
            Console.WriteLine("Patching Races.");
            state.PatchMod.Races.Set(
                state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                    .Where(r => r.HasKeyword(Skyrim.Keyword.ActorTypeNPC)
                        && !r.EditorID.Equals("TestRace"))
                    .Select(r => r.DeepCopy())
                    .Do(r =>
                    {
                        Console.WriteLine($"{r.EditorID} BaseCarryWeight : {r.BaseCarryWeight} -> {baseCarryWeight}");
                        r.BaseCarryWeight = baseCarryWeight;
                    })
            );
            Console.WriteLine("Paching Races Done.");

            Console.WriteLine("Patching Spells.");
            var magicEffects = MagicEffects(state);
            var carryWeightEffects = magicEffects.Item1;


            var healthMagicEffects = magicEffects.Item2;

            var carryWeightSpells = new HashSet<(IFormLinkGetter<ISpellGetter>, int)>();

            foreach (var spell in state.LoadOrder.PriorityOrder.Spell().WinningOverrides())
            {
                var deepCopySpell = spell.DeepCopy();
                foreach (var e in deepCopySpell.Effects)
                {
                    if (carryWeightEffects.Contains(e.BaseEffect))
                    {
                        Console.WriteLine($"{spell.EditorID} Magnitude: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                        e.Data.Magnitude *= effectMultiplyer;
                        carryWeightSpells.Add((spell.AsLink(), (int)e.Data.Magnitude));
                        deepCopySpell.Description += $"\n This alters your inventory space by {e.Data.Magnitude} Slots.";
                        state.PatchMod.Spells.Set(deepCopySpell);
                    }
                }
            }; 
            Console.WriteLine("Patching Spells Done.");

            var carryWeightSpellsItem1FormKeys = carryWeightSpells.Select(x => x.Item1.FormKey).ToHashSet();

            Console.WriteLine("Patching Perk Descriptions.");
            foreach (var perk in state.LoadOrder.PriorityOrder.Perk().WinningOverrides())
            {
                foreach (var effect in perk.ContainedFormLinks)
                {
                    if (carryWeightSpellsItem1FormKeys.Contains(effect.FormKey))
                    {
                        var deepcopyPerk = perk.DeepCopy();
                        foreach (var carryWeightSpell in carryWeightSpells)
                        {
                            foreach (var e in perk.Effects)
                            {
                                if (perk.Effects.Any(e => e.ContainedFormLinks.Any(a => a.FormKey.Equals(carryWeightSpell.Item1.FormKey))))
                                {
                                    Console.WriteLine($"Patched {perk.EditorID} Description.");
                                    if (perk.Effects.Count > 1)
                                    {
                                        deepcopyPerk.Description += $"\n This results a Slots change by {carryWeightSpell.Item2}.";
                                    }
                                    else
                                    {
                                        deepcopyPerk.Description += $"\n This equals {carryWeightSpell.Item2} Slots.";
                                    }
                                }
                            }
                        }
                        state.PatchMod.Perks.Set(deepcopyPerk);
                    }
                }
            };
            Console.WriteLine("Patching Perk Descriptions done.");

            Console.WriteLine("Patching Misc. Items.");
            state.PatchMod.MiscItems.Set(
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            Console.WriteLine("Patching Misc. Items done.");

            Console.WriteLine("Patching Ingestibles.");
            foreach (var i in state.LoadOrder.PriorityOrder.Ingestible().WinningOverrides())
            {
                var deepCopyI = i.DeepCopy();
                if (i.HasKeyword(Skyrim.Keyword.VendorItemPotion))
                {
                    deepCopyI.Weight = potionWeights;
                }
                else if (!i.EditorID.Equals("dunSleepingTreeCampSap"))
                {
                    deepCopyI.Weight = 0.0f;
                }
                foreach (var carryWeightEffect in carryWeightEffects)
                {
                    foreach (var e in deepCopyI.Effects)
                    {
                        if (carryWeightEffect.Equals(e.BaseEffect))
                        {
                            Console.WriteLine($"{i.EditorID} Magnitude: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                            e.Data.Magnitude *= effectMultiplyer;
                        }
                    }

                }
                if (NoHealFromWeightless)
                {
                    foreach (var healthMagicEffect in healthMagicEffects)
                    {
                        foreach (var e in deepCopyI.Effects)
                        {
                            if (healthMagicEffect.Equals(e.BaseEffect) && (i.HasKeyword(Skyrim.Keyword.VendorItemFood) || i.HasKeyword(Skyrim.Keyword.VendorItemFoodRaw)))
                            {
                                deepCopyI.Remove(healthMagicEffect.FormKey);
                                Console.WriteLine($"{i.EditorID} removed Health Effect.");
                            }
                            if (healthMagicEffect.Equals(e.BaseEffect)
                            &&
                            !(i.HasKeyword(Skyrim.Keyword.VendorItemFood)
                            || i.HasKeyword(Skyrim.Keyword.VendorItemFoodRaw)
                            || i.HasKeyword(Skyrim.Keyword.VendorItemPotion)
                            || i.EditorID.Equals("dunSleepingTreeCampSap")))
                            {
                                e.Data.Magnitude = 0;
                                Console.WriteLine($"{i.EditorID} set Health Effect {healthMagicEffect.FormKey} to 0.");
                            }
                        }
                    }
                }

                state.PatchMod.Ingestibles.Set(deepCopyI);            
            }

            Console.WriteLine("Patching Ingestibles done.");


            Console.WriteLine("Patching Ingredients.");
            foreach (var i in state.LoadOrder.PriorityOrder.Ingredient().WinningOverrides())
            {           
                var deepCopyI = i.DeepCopy();
                deepCopyI.Weight = 0.0f;
                foreach (var carryWeightEffect in carryWeightEffects)
                {
                    foreach (var e in deepCopyI.Effects)
                    {
                        if (carryWeightEffect.Equals(e.BaseEffect))
                        {
                            Console.WriteLine($"{i.EditorID} Magnitude: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                            e.Data.Magnitude *= effectMultiplyer;
                        }
                    }
                
                }
                if (NoHealFromWeightless)
                {
                    foreach (var healthMagicEffect in healthMagicEffects)
                    {
                        foreach (var e in deepCopyI.Effects)
                        {
                            if (healthMagicEffect.Equals(e.BaseEffect))
                            {
                                e.Data.Magnitude = 0;
                                Console.WriteLine($"{i.EditorID} set Health Effect {healthMagicEffect.FormKey} to 0.");
                            }
                        }
                    }
                }
                state.PatchMod.Ingredients.Set(deepCopyI);            
            }
            Console.WriteLine("Patching Ingredients done.");

            Console.WriteLine("Patching Object Effects.");
            foreach (var i in state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides()) 
            {
                foreach (var carryWeightEffect in carryWeightEffects)
                {
                    var deepCopyI = i.DeepCopy();
                    foreach (var e in deepCopyI.Effects)
                    {
                        if (carryWeightEffect.Equals(e.BaseEffect))
                        {
                            Console.WriteLine($"{i.EditorID} {e.BaseEffect} Magnitude: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                            e.Data.Magnitude *= effectMultiplyer;
                            state.PatchMod.ObjectEffects.Set(deepCopyI);
                        }
                    }
                }
            }
            Console.WriteLine("Patching Object Effects done.");

            Console.WriteLine("Patching Books.");
            state.PatchMod.Books.Set(
                state.LoadOrder.PriorityOrder.Book().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            Console.WriteLine("Patching Books done.");

            Console.WriteLine("Patching Ammunitions.");
            state.PatchMod.Ammunitions.Set(
                state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            Console.WriteLine("Patching Ammunitions done.");

            Console.WriteLine("Patching Soul Gems.");
            state.PatchMod.SoulGems.Set(
                state.LoadOrder.PriorityOrder.SoulGem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            Console.WriteLine("Patching Soul Gems done.");

            var weaponWeights = state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()
                .Where(w => w.BasicStats?.Weight != 0)
                .Select(w => w.BasicStats?.Weight ?? 0.0f);
            Console.WriteLine("Making Weapon Distributions: ");
            var weaponDistributions = MakeDistributions(weaponWeights);
            state.PatchMod.Weapons.Set(
                state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()
                    .Where(w => w.BasicStats?.Weight != 0 && w.BasicStats?.Weight != FindWeight(weaponDistributions, w.BasicStats!.Weight))
                    .Select(m => m.DeepCopy())
                    .Do(w =>
                    {
                        var weight = FindWeight(weaponDistributions, w.BasicStats!.Weight);
                        Console.WriteLine($"{w.Name} : {w.BasicStats!.Weight} -> {weight}");
                        w.BasicStats!.Weight = weight;
                    })
                
            );
            
            var armorWeights = state.LoadOrder.PriorityOrder.Armor().WinningOverrides()
                .Where(w => w.Weight != 0)
                .Select(w => w.Weight);
            Console.WriteLine("Making Armor Distributions: ");
            var armorDistributions = MakeDistributions(armorWeights);
            state.PatchMod.Armors.Set(
                state.LoadOrder.PriorityOrder.Armor().WinningOverrides()
                    .Where(w => w.Weight != 0 && w.Weight != FindWeight(weaponDistributions, w.Weight))
                    .Select(m => m.DeepCopy())
                    .Do(w =>
                    {
                        var weight = FindWeight(weaponDistributions, w.Weight);
                        Console.WriteLine($"{w.Name} : {w.Weight} -> {weight}");
                        w.Weight = weight;
                    })
                
            );
            
        }

        private static float FindWeight(List<(float MaxWeight, int Slots)> distributions, float weight)
        {
            var found = distributions.FirstOrDefault(d => d.MaxWeight >= weight);
            if (found == default) 
                found = distributions.Last();
            return found.Slots;
        }

        private static List<(float MaxWeight, int Slots)> MakeDistributions(IEnumerable<float> weights, int minSlots = 1, int maxSlots = 5)
        {
            var warr = weights.ToArray();
            var deltaSlots = maxSlots - minSlots;
            var minWeight = (float)warr.Min();
            var maxWeight = (float)warr.Max();
            var deltaWeight = maxWeight - minWeight;
            var sectionSize = deltaWeight / (deltaSlots + 1);

            var output = new List<(float MaxWeight, int Slots)>();
            var weight = minWeight + sectionSize;
            for (var slots = minSlots; slots <= maxSlots; slots += 1)
            {
                Console.WriteLine($" - {weight} -> {slots}");
                output.Add((weight, slots));
                weight += sectionSize;
            }

            return output;
        }

        private static (HashSet<IFormLinkGetter<IMagicEffectGetter>>, HashSet<IFormLinkGetter<IMagicEffectGetter>>) MagicEffects(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var foundCarryWeight = new HashSet<IFormLinkGetter<IMagicEffectGetter>>();
            var foundHealth = new HashSet<IFormLinkGetter<IMagicEffectGetter>>();
            foreach (var e in state.LoadOrder.PriorityOrder.MagicEffect().WinningOverrides())
            {
                if (e.Archetype.ActorValue.Equals(ActorValue.CarryWeight))
                {
                    foundCarryWeight.Add(e.AsLink());
                }
                if (e.Archetype.ActorValue.Equals(ActorValue.Health)
                    && !e.Flags.HasFlag(MagicEffect.Flag.Hostile)
                    && !e.Description.String.IsNullOrWhitespace())
                {
                    foundHealth.Add(e.AsLink());
                }
            }
            return (foundCarryWeight, foundHealth);
        }
    }
}