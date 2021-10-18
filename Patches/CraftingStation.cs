using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CraftyCartsRemake.Patches
{
    [Harmony]
    internal class CraftingStation_Patch
    {
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        [HarmonyPostfix]
        private static void CCRCraftingStation_Start(CraftingStation __instance,
            ref List<CraftingStation> ___m_allStations)
        {
            if (__instance.name != "CraftyCarts.CraftingStation") return;
            if (!___m_allStations.Contains(__instance))
                ___m_allStations.Add(__instance);
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.FixedUpdate))]
        [HarmonyPostfix]
        private static void CCRCraftingStation_FixedUpdate(CraftingStation __instance, ref float ___m_useTimer,
            ref float ___m_updateExtensionTimer, GameObject ___m_inUseObject)
        {
            if (__instance.name != "CraftyCarts.CraftingStation") return;
            ___m_useTimer += Time.fixedDeltaTime;
            ___m_updateExtensionTimer += Time.fixedDeltaTime;
            if (___m_inUseObject) ___m_inUseObject.SetActive(___m_useTimer < 1f);
        }
    }
}