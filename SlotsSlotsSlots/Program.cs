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
            return await SynthesisPipeline.Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch).Run(args, new RunPreferences()
            {
                ActionsForEmptyArgs = new RunDefaultPatcher()
                {
                    IdentifyingModKey = "SlotsSlotsSlots.esp",
                    TargetRelease = GameRelease.SkyrimSE,
                }
            });
            
        }
        
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            state.PatchMod.Races.Set(
                state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                    .Where(r => r.HasKeyword(Skyrim.Keyword.ActorTypeNPC) 
                                             && !(r.EditorID.Equals("InvisibleRace")
                                                || r.EditorID.Equals("ElderRace")
                                                || r.EditorID.Equals("ElderRaceVampire")
                                                || r.HasKeyword(Skyrim.Keyword.ActorTypeDaedra)
                                                ))
                    .Select(r => r.DeepCopy())
                    .Do(r =>
                    {
                        r.BaseCarryWeight = 25.0f;
                        Console.WriteLine($"Set BaseCarryWeight for {r.Name} to {r.BaseCarryWeight}");
                    })
            );

            state.PatchMod.MiscItems.Set(
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Ingestibles.Set(
                state.LoadOrder.PriorityOrder.Ingestible().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Ingredients.Set(
                state.LoadOrder.PriorityOrder.Ingredient().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Books.Set(
                state.LoadOrder.PriorityOrder.Book().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            
            state.PatchMod.Ammunitions.Set(
                state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));
            
            state.PatchMod.SoulGems.Set(
                state.LoadOrder.PriorityOrder.SoulGem().WinningOverrides()
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            var weaponWeights = state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()
                .Where(w => w.BasicStats?.Weight != 0)
                .Select(w => w.BasicStats?.Weight ?? 0.0f);
            Console.WriteLine("Making Weapon Distributions: ");
            var weaponDistributions = MakeDistributions(weaponWeights);
            state.PatchMod.Weapons.Set(
                state.LoadOrder.PriorityOrder.Weapon().WinningOverrides()
                    .Where(w => w.BasicStats?.Weight != 0)
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
                    .Where(w => w.Weight != 0)
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

    }
}