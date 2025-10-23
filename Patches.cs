using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CraftyCartsRemake.CCR;

namespace CraftyCartsRemake;

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
static class CCR_EnsureCartIsInList
{
    static void Postfix(CraftingStation __instance, ref List<CraftingStation> ___m_allStations)
    {
        if (__instance.name is not (ForgeCartInstance or StoneCartInstance or WorkbenchCartInstance or CauldronCartInstance or BlackForgeCartInstance or ArtisanCartInstance)) return;
        if (__instance.m_nview && __instance.m_nview.GetZDO() == null)
            return;
        if (___m_allStations.Contains(__instance)) return;
        ___m_allStations.Add(__instance);
    }
}

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CustomUpdate))]
static class CCR_MakeCartInUseObjectVisInvis
{
    static void Postfix(CraftingStation __instance, ref float ___m_useTimer, ref float ___m_updateExtensionTimer, GameObject ___m_inUseObject)
    {
        if (__instance.name is not (ForgeCartInstance or StoneCartInstance or WorkbenchCartInstance or CauldronCartInstance or BlackForgeCartInstance or ArtisanCartInstance)) return;
        ___m_useTimer += Time.fixedDeltaTime;
        ___m_updateExtensionTimer += Time.fixedDeltaTime;
        if (___m_inUseObject) ___m_inUseObject.SetActive(___m_useTimer < 1f);
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.Awake))]
static class CCR_ChangeContainerInventorySize
{
    static void Postfix(Container __instance, ref Inventory ___m_inventory)
    {
        if (__instance.m_nview.GetZDO() == null || !__instance.m_nview.IsValid())
            return;
        string cartName = __instance.transform.root.name.Replace("(Clone)", "").Trim();

        ref int inventoryColumns = ref ___m_inventory.m_width;
        ref int inventoryRows = ref ___m_inventory.m_height;
        switch (cartName)
        {
            // Forge Cart Storage
            case ForgeCartFabName:
                inventoryRows = ForgeCartRow.Value;
                inventoryColumns = ForgeCartCol.Value;
                break;
            // Stonecutter Cart Storage
            case StoneCartFabName:
                inventoryRows = StonecutterCartRow.Value;
                inventoryColumns = StonecutterCartCol.Value;
                break;
            // Workbench Cart Storage
            case WorkbenchCartFabName:
                inventoryRows = WorkbenchCartRow.Value;
                inventoryColumns = WorkbenchCartCol.Value;
                break;
            // Cauldron Car Storage
            case CauldronCartFabName:
                inventoryRows = CauldronCartRow.Value;
                inventoryColumns = CauldronCartCol.Value;
                break;
            case BlackForgeCartFabName:
                inventoryRows = BlackForgeCartRow.Value;
                inventoryColumns = BlackForgeCartCol.Value;
                break;
            case ArtisanCartFabName:
                inventoryRows = ArtisanCartRow.Value;
                inventoryColumns = ArtisanCartCol.Value;
                break;
        }
    }
}

[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.CollideRay2))]
[HarmonyPriority(Priority.VeryHigh)]
public class CCR_NoCameraClippingWithCart
{
    static bool Prefix() => !(Player.m_localPlayer && IsPlayerAttachedToVagon(Player.m_localPlayer));

    internal static bool IsPlayerAttachedToVagon(Character player) => Vagon.m_instances.Any(vagon => vagon && vagon.IsAttached(player));
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    [HarmonyPriority(Priority.Last)]
    static void Postfix(ZNetScene __instance)
    {
        FixReferences(__instance, "piece_workbench", WorkbenchCartFabName);
        FixReferences(__instance, "forge", ForgeCartFabName);
        FixReferences(__instance, "piece_stonecutter", StoneCartFabName);
        FixReferences(__instance, "piece_cauldron", CauldronCartFabName);
        FixReferences(__instance, "blackforge", BlackForgeCartFabName);
        FixReferences(__instance, "piece_artisanstation", ArtisanCartFabName);
    }


    private static void FixReferences(ZNetScene zns, string originalBench, string prefabToFix)
    {
        GameObject? origbench = zns.GetPrefab(originalBench);
        GameObject? newBench = zns.GetPrefab(prefabToFix);
        if (!origbench || !newBench) return;

        CraftyCart? cart = newBench.GetComponent<CraftyCart>();
        if (!cart) return;

        WearNTear? wnt = origbench.GetComponentInChildren<WearNTear>();
        if (wnt)
        {
            cart.m_wearNTear.m_destroyedEffect = wnt.m_destroyedEffect;
            cart.m_wearNTear.m_hitEffect = wnt.m_hitEffect;
            cart.m_wearNTear.m_switchEffect = wnt.m_switchEffect;
        }

        if (cart.m_container)
            cart.m_container.m_destroyedLootPrefab = zns.GetPrefab("CargoCrate");

        CraftingStation? station = origbench.GetComponentInChildren<CraftingStation>();
        if (station)
        {
            cart.m_craftingStation.m_craftItemEffects = station.m_craftItemEffects;
            cart.m_craftingStation.m_craftItemDoneEffects = station.m_craftItemDoneEffects;
            cart.m_craftingStation.m_repairItemDoneEffects = station.m_craftItemEffects;
        }

        Piece? piece = origbench.GetComponentInChildren<Piece>();
        if (piece)
            cart.m_piece.m_placeEffect = piece.m_placeEffect;

        SmokeSpawner? smokeSpawner = origbench.GetComponentInChildren<SmokeSpawner>();
        if (cart.m_smokeSpawner && smokeSpawner)
            cart.m_smokeSpawner.m_smokePrefab = smokeSpawner.m_smokePrefab;

        CircleProjector? circleProjector = origbench.GetComponentInChildren<CircleProjector>();
        if (circleProjector && cart.m_projector)
            cart.m_projector.m_prefab = circleProjector.m_prefab;

        // Fix the animation controller for the artisan cart.
        if (prefabToFix == ArtisanCartFabName)
        {
            GameObject? extensionToFix = zns.GetPrefab("artisan_ext1");
            Animator? animator = extensionToFix.GetComponentInChildren<Animator>();
            if (animator == null || cart.upgradeVisualsParent.GetChild(0) == null) return;
            Animator? cartAnimator = cart.upgradeVisualsParent.GetChild(0).GetComponentInChildren<Animator>();
            if (cartAnimator != null)
            {
                cartAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
            }
        }
    }
}

[HarmonyPatch(typeof(Vagon), nameof(Vagon.UpdateMass))]
public static class Vagon_UpdateMass_Patch
{
    static void Postfix(Vagon __instance)
    {
        Inventory? inv = __instance.m_container ? __instance.m_container.GetInventory() : null;
        if (inv == null || __instance.m_nview == null || !__instance.m_nview.IsOwner()) return;

        float totalWeight = inv.GetTotalWeight();

        // Adjust the center of mass based on the weight
        Vector3 newCenterOfMass = __instance.m_body.centerOfMass;
        // Lower the center of mass if the cart is light or adjust center of mass based on the current weight
        newCenterOfMass.y = (totalWeight < 10) ? 0.2f : 1f - Mathf.Clamp(totalWeight * 0.05f, 0f, 1f);

        __instance.m_body.centerOfMass = newCenterOfMass;
    }
}

[HarmonyPatch(typeof(Vagon), nameof(Vagon.FixedUpdate))]
public static class Vagon_FixedUpdate_Patch
{
    static void Postfix(Vagon __instance)
    {
        var rigidBody = __instance.m_body;
        var inv = __instance.m_container ? __instance.m_container.GetInventory() : null;
        if (!rigidBody || inv == null || !__instance.m_nview || !__instance.m_nview.IsOwner()) return;
        // Adjust the angular drag based on the weight
        float totalWeight = inv.GetTotalWeight();
        rigidBody.angularDamping = Mathf.Clamp(0.05f + totalWeight * 0.01f, 0.5f, 5f);
    }
}

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetExtentionCount))]
public static class CraftingStation_GetExtensionCount_Patch
{
    static void Postfix(CraftingStation __instance, bool checkExtensions, ref int __result)
    {
        // Check if this station is part of a cart by trying to get the CraftyCart component.
        CraftyCart cart = __instance.GetComponentInParent<CraftyCart>();
        if (!cart) return;
        // For example, if currentUpgradeLevel is 1 then bonus is 0,
        // if currentUpgradeLevel is 3 then bonus is 2.
        int bonusExtensions = Mathf.Max(cart.currentUpgradeLevel - 1, 0);
        __result += bonusExtensions;
    }
}

[HarmonyPatch(typeof(Sign), nameof(Sign.Awake))]
public static class Sign_Awake_Transpiler
{
    /// <summary>
    /// Transpiler that replaces the call to GetComponent<ZNetView>() with a helper method.
    /// </summary>
    /// <param name="instructions">The original IL instructions.</param>
    /// <returns>The modified IL instructions.</returns>
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Convert instructions to a list for easier modifications.
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

        // Look for the call to GetComponent<ZNetView>().
        // Original IL pattern:
        //   ldarg.0      // target for field assignment
        //   ldarg.0      // receiver for GetComponent call
        //   call instance ZNetView GetComponent<ZNetView>()
        //   stfld ZNetView Sign::m_nview
        //
        // We want to change the call to:
        //   ldarg.0      // target for field assignment
        //   ldarg.0      // parameter for helper
        //   call static ZNetView GetComponentFromSelfOrParent(Component)
        //   stfld ZNetView Sign::m_nview
        for (int i = 0; i < codes.Count; ++i)
        {
            if (codes[i].opcode == OpCodes.Call)
            {
                MethodInfo method = codes[i].operand as MethodInfo;
                if (method != null && method.IsGenericMethod)
                {
                    // Check if the generic argument is ZNetView.
                    Type[] genericArgs = method.GetGenericArguments();
                    if (genericArgs.Length == 1 && genericArgs[0] == typeof(ZNetView))
                    {
                        // Replace the call operand with a helper method.
                        MethodInfo helperMethod = AccessTools.Method(typeof(Sign_Awake_Transpiler), nameof(GetComponentFromSelfOrParent));
                        codes[i].operand = helperMethod;
                        // Set opcode to Call, since the helper is a static method.
                        codes[i].opcode = OpCodes.Call;

                        // Don't remove any ldarg.0; both are needed.
                        break;
                    }
                }
            }
        }

        return codes;
    }

    /// <summary>
    /// Helper method that retrieves a ZNetView component.
    /// It first checks the current GameObject, and if none is found and the immediate parent
    /// is named "BumperSticker", it searches all parents for a ZNetView.
    /// </summary>
    /// <param name="self">The component instance (typically 'this' from Sign).</param>
    /// <returns>The found ZNetView component, or null if none exists.</returns>
    public static ZNetView GetComponentFromSelfOrParent(Component self)
    {
        // If the immediate parent is named "BumperSticker", always get the component from the parent.
        if (self.transform.parent && self.transform.parent.gameObject.name == "BumperSticker")
        {
            // Search the parent's hierarchy for a ZNetView.
            return self.transform.parent.GetComponentInParent<ZNetView>();
        }

        // Otherwise, just use the component on the current GameObject.
        return self.GetComponent<ZNetView>();
    }
}

[HarmonyPatch(typeof(Sign), nameof(Sign.Awake))]
static class SignAwakePatch2
{
    static void Prefix(Sign __instance)
    {
        if (__instance.transform.parent == null || __instance.transform.parent.gameObject.name != "BumperSticker") return;
        Transform? textGameObject = Utils.FindChild(__instance.transform, "Text");
        if (!textGameObject) return;
        Text? oldTextField = textGameObject.GetComponent<Text>();
        if (!oldTextField) return;

        // Create a new GameObject to hold the TextMeshProUGUI component
        GameObject newTextMeshProGameObject = new GameObject("Text");
        newTextMeshProGameObject.transform.SetParent(textGameObject.transform.parent);

        // Just in case it has a different scale and shit
        Transform transform = textGameObject.transform;
        newTextMeshProGameObject.transform.localPosition = transform.localPosition;
        newTextMeshProGameObject.transform.localRotation = transform.localRotation;
        newTextMeshProGameObject.transform.localScale = transform.localScale;

        TextMeshProUGUI? textMeshPro = newTextMeshProGameObject.AddComponent<TextMeshProUGUI>();
        textMeshPro.text = oldTextField.text;
        textMeshPro.font = TMP_FontAsset.CreateFontAsset(oldTextField.font);
        textMeshPro.fontSize = oldTextField.fontSize;
        textMeshPro.color = oldTextField.color;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.textWrappingMode = TextWrappingModes.PreserveWhitespace;

        // Replace the field value
        __instance.m_textWidget = textMeshPro;

        // Remove the old Text component
        UnityEngine.Object.Destroy(oldTextField.gameObject);
    }

    static void Postfix(Sign __instance)
    {
        // Ensure the Sign script remains enabled, when testing, it was disabled for some reason
        if (!__instance.enabled)
            __instance.enabled = true;
    }
}