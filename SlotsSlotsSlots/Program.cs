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

            state.PatchMod.Races.Set(
                state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                    .Where(r => r.Flags.HasFlag(Race.Flag.Playable))
                    .Select(r => r.DeepCopy())
                    .Do(r =>
                    {
                        r.BaseCarryWeight = 25.0f;
                        Console.WriteLine($"Set BaseCarryWeight for {r.Name} to {r.BaseCarryWeight}");
                    })
            );

            var carryWeightEffects = carryWeightMagicEffects(state);
            var carryWeightSpells = new HashSet<(IFormLinkGetter<ISpellGetter>, string)>();
            state.PatchMod.Spells.Set(state.LoadOrder.PriorityOrder.Spell().WinningOverrides()
                .Where(spell =>
                {
                    foreach (var carryWeightEffect in carryWeightEffects)
                    {
                        if (spell.Effects.Any(e => e.BaseEffect.Equals(carryWeightEffect))) return true;
                    }
                    return false;
                })
                .Select(s => s.DeepCopy())
                .Do(spell =>
                {
                    foreach (var carryWeightEffect in carryWeightEffects)
                    {
                        spell.Effects.Do(e =>
                        {
                            if (e.BaseEffect.Equals(carryWeightEffect))
                            {
                                e.Data.Magnitude /= 6;
                                string changeNote = $"\n This alters your inventory space by {e.Data.Magnitude} Slots.";
                                carryWeightSpells.Add((spell.AsLink(), changeNote));
                                spell.Description += changeNote;
                            }
                        });
                    }
                }));

            state.PatchMod.Perks.Set(
                state.LoadOrder.PriorityOrder.Perk().WinningOverrides()
                    .Where(p =>
                    {
                        foreach (var carryWeightSpell in carryWeightSpells)
                        {
                            if (p.Effects.Any(e => e.ContainedFormLinks.Any(a => a.FormKey.Equals(carryWeightSpell.Item1)))) return true;
                        }
                        return false;
                    }
                    )
                    .Select(p => p.DeepCopy())
                    .Do(p =>
                    {
                        foreach (var carryWeightSpell in carryWeightSpells)
                        {
                            p.Effects.Do(e =>
                            {
                                if (p.Effects.Any(e => e.ContainedFormLinks.Any(a => a.FormKey.Equals(carryWeightSpell.Item1))))
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

            state.PatchMod.MiscItems.Set(
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Ingestibles.Set(
                state.LoadOrder.PriorityOrder.Ingestible().WinningOverrides()
                    .Where(i => i.Weight != 0.0f
                        || i.Effects
                        .Any(e =>
                        {
                            foreach (var carryWeightEffect in carryWeightEffects)
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect)) return true;
                            }
                            return false;
                        }))
                    .Select(i => i.DeepCopy())
                    .Do(i => 
                    {
                        i.Weight = 0.0f;
                        i.Keywords.Do(k => 
                        {
                            if (k.Equals(Skyrim.Keyword.VendorItemPotion)) i.Weight = 0.1f;
                        });
                        
                        foreach (var carryWeightEffect in carryWeightEffects)
                        {
                            i.Effects.Do(e =>
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect))
                                {
                                    e.Data.Magnitude /= 6;
                                }
                            });
                        }
                    }));

            state.PatchMod.ObjectEffects.Set(
                state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides()
                    .Where(i => i.Effects
                        .Any(e =>
                        {
                            foreach (var carryWeightEffect in carryWeightEffects)
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect)) return true;
                            }
                            return false;
                        }))
                    .Select(m => m.DeepCopy())
                    .Do(i =>
                    {
                        foreach (var carryWeightEffect in carryWeightEffects)
                        {
                            i.Effects.Do(e =>
                            {
                                if (e.BaseEffect.Equals(carryWeightEffect))
                                {
                                    e.Data.Magnitude /= 6;
                                }
                            });
                        }
                    }));

            state.PatchMod.Ingredients.Set(
                state.LoadOrder.PriorityOrder.Ingredient().WinningOverrides()
                    .Where(i => i.Weight != 0.0f
                        || i.Effects
                        .Any(e =>
                        {
                            foreach (var carryWeightEffect in carryWeightEffects)
                            {
                                if ( e.BaseEffect.Equals(carryWeightEffect)) return true;
                            }
                            return false;
                        }))
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
                                    e.Data.Magnitude /= 6;
                                }
                            });
                        }
                    }));

            state.PatchMod.Books.Set(
                state.LoadOrder.PriorityOrder.Book().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            
            state.PatchMod.Ammunitions.Set(
                state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            
            state.PatchMod.SoulGems.Set(
                state.LoadOrder.PriorityOrder.SoulGem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

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

        private static HashSet<IFormLinkGetter<IMagicEffectGetter>> carryWeightMagicEffects(IPatcherState<ISkyrimMod, ISkyrimModGetter>  state)
        {
            var foundEffects = new HashSet<IFormLinkGetter<IMagicEffectGetter>>();
            state.LoadOrder.PriorityOrder.MagicEffect().WinningOverrides()
                .Where(e => e.Archetype.ActorValue.Equals("Carry Weight"))
                .Do(e =>
                {
                    foundEffects.Add(e.AsLink());
                });
            return foundEffects;
        }

    }
}