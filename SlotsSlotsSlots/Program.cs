using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            float effectMultiplyer = 0.4f;
            float potionWeights = 0.1f;
            bool foodNoHeal = true;

            state.PatchMod.Races.Set(
                state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                    .Where(r => r.Flags.HasFlag(Race.Flag.Playable))
                    .Select(r => r.DeepCopy())
                    .Do(r =>
                    {
                        Console.WriteLine($"BaseCarryWeight {r.Name} : {r.BaseCarryWeight} -> {baseCarryWeight}");
                        r.BaseCarryWeight = baseCarryWeight;
                    })
            );

            Console.WriteLine("Patching Spells:");

            var carryWeightEffects = MagicEffects(state).Item1;
            var carryWeightEffectFormKeys = carryWeightEffects.Select(x => x.FormKey).ToHashSet();

            var healthMagicEffects = MagicEffects(state).Item2;

            var carryWeightSpells = new HashSet<(IFormLinkGetter<ISpellGetter>, int)>();

            state.PatchMod.Spells.Set(state.LoadOrder.PriorityOrder.Spell().WinningOverrides()
                .Where(spell => spell.Effects.Any(e => carryWeightEffectFormKeys.Contains(e.BaseEffect.FormKey)))
                .Select(s => s.DeepCopy())
                .Do(spell =>
                {
                    foreach (var carryWeightEffect in carryWeightEffectFormKeys)
                    {
                        Console.WriteLine($"{carryWeightEffect}");
                        spell.Effects.Do(e =>
                        {
                            if (e.BaseEffect.FormKey.Equals(carryWeightEffect))
                            {
                                Console.WriteLine($"{spell.Name} Strenth: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                                e.Data.Magnitude *= effectMultiplyer;
                                carryWeightSpells.Add((spell.AsLink(), (int)e.Data.Magnitude));
                                spell.Description += $"\n This alters your inventory space by {e.Data.Magnitude} Slots.";
                            }
                        });
                    }
                }));

            var carryWeightSpellsItem1 = carryWeightSpells.Select(x => x.Item1).ToHashSet();

            Console.WriteLine("Patching Perk Descriptions.");
            state.PatchMod.Perks.Set(
                state.LoadOrder.PriorityOrder.Perk().WinningOverrides()
                    .Where(p => //This could potentially be shortened.
                    {
                        foreach (var carryWeightSpell in carryWeightSpells)
                        {
                            if (p.Effects.Any(e => e.ContainedFormLinks.Any(a => a.FormKey.Equals(carryWeightSpell.Item1.FormKey)))) return true;
                        }
                        return false;
                    })
                    .Select(p => p.DeepCopy())
                    .Do(p =>
                    {
                        foreach (var carryWeightSpell in carryWeightSpells)
                        {
                            p.Effects.Do(e =>
                            {
                                if (p.Effects.Any(e => e.ContainedFormLinks.Any(a => a.FormKey.Equals(carryWeightSpell.Item1.FormKey))))
                                {
                                    if (p.Effects.Count > 1)
                                    {
                                        p.Description += $"\n This results a Slots change by {carryWeightSpell.Item2}.";
                                    }
                                    else
                                    {
                                        p.Description += $"\n This equals {carryWeightSpell.Item2} Slots.";
                                    }
                                }
                            });
                        }
                    }));
            Console.WriteLine("Patching Perk Descriptions done.");

            Console.WriteLine("Patching Misc. Items.");
            state.PatchMod.MiscItems.Set(
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            Console.WriteLine("Patching Misc. Items done.");

            Console.WriteLine("Patching Ingestibles.");
            state.PatchMod.Ingestibles.Set(
                state.LoadOrder.PriorityOrder.Ingestible().WinningOverrides()
                    .Where(i => i.Weight != 0.0f
                        || i.Effects.Any(e => carryWeightEffects.Contains(e.BaseEffect) || healthMagicEffects.Contains(e.BaseEffect)))
                    .Select(i => i.DeepCopy())
                    .Do(i => 
                    {
                        i.Weight = 0.0f;
                        i.Keywords.Do(k => 
                        {
                            if (k.Equals(Skyrim.Keyword.VendorItemPotion)) i.Weight = potionWeights;
                        });
                        
                        foreach (var carryWeightEffect in carryWeightEffects)
                        {
                            i.Effects.Do(e =>
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect))
                                {
                                    Console.WriteLine($"{i.Name} Strenth: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                                    e.Data.Magnitude *= effectMultiplyer;
                                }

                                if (i.MajorFlags.HasFlag(Ingestible.Flag.FoodItem) && foodNoHeal)
                                {
                                    foreach (var healtMagicEffect in healthMagicEffects)
                                    {
                                        if (e.BaseEffect.Equals(healtMagicEffect))
                                        {
                                            i.Remove(healtMagicEffect.FormKey);
                                            Console.WriteLine($"{i.Name} removed Health Effect.");
                                        }
                                    }
                                }
                            });
                        }
                    }));
            Console.WriteLine("Patching Ingestibles done.");

            Console.WriteLine("Patching Object Effects.");
            state.PatchMod.ObjectEffects.Set(
                state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides()
                    .Where(i => i.Effects.Any(e => carryWeightEffects.Contains(e.BaseEffect)))
                    .Select(m => m.DeepCopy())
                    .Do(i =>
                    {                        
                        i.Effects.Do(e =>
                        {
                            if (carryWeightEffects.Contains(e.BaseEffect))
                            { 
                                Console.WriteLine($"{i.Name} Strenth: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                                e.Data.Magnitude *= effectMultiplyer;
                            }
                        });
                    }));
            Console.WriteLine("Patching Object Effects done.");

            Console.WriteLine("Patching Ingredients.");
            state.PatchMod.Ingredients.Set(
                state.LoadOrder.PriorityOrder.Ingredient().WinningOverrides()
                    .Where(i => i.Weight != 0.0f
                        || i.Effects.Any(e => carryWeightEffects.Contains(e.BaseEffect)))
                    .Select(i => i.DeepCopy())
                    .Do(i =>
                    {
                        i.Weight = 0.0f;
                        foreach (var carryWeightEffect in carryWeightEffects)
                        {
                            i.Effects.Do(e =>
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect))
                                {
                                    Console.WriteLine($"{i.Name} Strenth: {e.Data.Magnitude} -> {e.Data.Magnitude * effectMultiplyer}");
                                    e.Data.Magnitude *= effectMultiplyer;
                                }
                            });
                        }
                    }));
            Console.WriteLine("Patching Ingredients done.");

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
            state.LoadOrder.PriorityOrder.MagicEffect().WinningOverrides()
                .Do(e =>
                {
                    if (e.Archetype.ActorValue.Equals(ActorValue.CarryWeight))
                    {
                        foundCarryWeight.Add(e.AsLink());
                    }
                    if (e.Archetype.ActorValue.Equals(ActorValue.Health))
                    {
                        foundHealth.Add(e.AsLink());
                    }
                });
            return (foundCarryWeight, foundHealth);
        }
    }
}