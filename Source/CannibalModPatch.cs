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
            ingester.story.traits.GainTrait(new Trait(TraitDefOf.Cannibal, 0, true));
            /* Messages.Message("TROLL", ingester, MessageTypeDefOf.NeutralEvent);
            Find.WindowStack.Add(new Dialog_MessageBox(ingester.Name + " just turned cannibalist."));
            var semTex = new Dialog_MessageBox(ingester.Name + " just turned cannibalist.");
            semTex.windowRect.width = 150;
            semTex.windowRect.height = 100;
            Find.WindowStack.Add(semTex); */
        }
    }
}