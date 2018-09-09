using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Harmony;
using RimWorld;

namespace CannibalModPatch {

    [StaticConstructorOnStartup]
    public class Main {

        static Main() {
            var harmony = HarmonyInstance.Create("com.github.eviltactician.rimworld.mod.cannibalmod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Main.LogMessage("Initialised");
        }
        public static void LogMessage(string text) {
            Log.Message("[CannibalMod] " + text);
        }
    }

    [HarmonyPatch(typeof(Thing))]
    [HarmonyPatch("Ingested")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(float) })] //might not need this
    public class CannibalTraitPatch {

        public static bool IsHumanLike(Thing thing) {
            bool isHumanLike = false;
            if (FoodUtility.IsHumanlikeMeat(thing.def)) {
                isHumanLike = true;
            } else {
                CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                if (compIngredients != null) {
                    foreach (var ing in compIngredients.ingredients) {
                        if (FoodUtility.IsHumanlikeMeat(ing)) {
                            isHumanLike = true;
                            break;
                        }
                    }
                }
            }
            return isHumanLike;
        }
        [HarmonyPostfix]
        public static void PawnHasEaten(Thing __instance, float __result, Pawn ingester, float nutritionWanted) {
            if (__result <= 0) {
                return;
            }
            if (!ingester.RaceProps.Humanlike) {
                return;
            }
            if (ingester.story.traits.HasTrait(TraitDefOf.Cannibal)) {
                return;
            }
            if (!IsHumanLike(__instance)) {
                return;
            }
            // Set base chance
            float chance = 0.01f; // base chance 1%
            
            // Start checking for existing traits to determine chance modifiers
            if (ingester.story.traits.HasTrait(TraitDefOf.Psychopath)) {
                chance += chance + 0.05f; // Psychopath +5% base chance
            }
            if (ingester.story.traits.HasTrait(TraitDefOf.Bloodlust)) {
                chance += chance + 0.025f; // Bloodlust +2.5% base chance
            }
            if (ingester.story.traits.HasTrait(TraitDefOf.Kind)) {
                chance = chance * 0.25f; // Kind 75% lower base chance
            }
            // not working - how to check if a pawn is incapable of violence?
            // var disabled = ingester.story.childhood.DisabledWorkTypes(WorkTypeDef disabled);
            /*
            var noViolence = ingester.story.childhood.workDisables;

            if (noViolence.Contains("Violence") {
                chance = 0f; // No Violence lowers base chance by 100%
            } */

            // roll some "dice" - get a value between 1 and 100 pct
            float roll = UnityEngine.Random.Range(0.0f, 1.0f);

            // now test the roll vs. the chance set previously and add trait if this is relevant
            if (roll < chance) {
                ingester.story.traits.GainTrait(new Trait(TraitDefOf.Cannibal, 0, true));
                // temporary message for debugging purposes
                Messages.Message("At a chance of " + chance + " and a roll of " + roll + ", " + ingester.Name + " just gained the cannibal trait.", ingester, MessageTypeDefOf.NeutralEvent); 
            }
            // Debugging, I want to see what happens even if the roll "fails"
            else {
                Messages.Message("At a chance of " + chance + " and a roll of " + roll + ", " + ingester.Name + " did NOT gain the cannibal trait.", ingester, MessageTypeDefOf.NeutralEvent);
            }

            // TO-DO - distinguish between colonist and prisoner


            // TO-DO - Sort better Messaging to End-User

            /* Messages.Message("TROLL", ingester, MessageTypeDefOf.NeutralEvent);
            Find.WindowStack.Add(new Dialog_MessageBox(ingester.Name + " just turned cannibalist."));
            var semTex = new Dialog_MessageBox(ingester.Name + " just turned cannibalist.");
            semTex.windowRect.width = 150;
            semTex.windowRect.height = 100;
            Find.WindowStack.Add(semTex); */
        }
    }
}