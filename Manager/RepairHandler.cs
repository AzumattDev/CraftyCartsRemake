using HarmonyLib;
using UnityEngine;

namespace CraftyCartsRemake;

[HarmonyPatch(typeof(Player), nameof(Player.Repair))]
public static class UpgradeCartRepairPatch
{
    static bool Prefix(Player __instance, ItemDrop.ItemData toolItem, ref Piece repairPiece)
    {
        if (!__instance.InPlaceMode())
            return true;

        Piece hoveringPiece = __instance.GetHoveringPiece();
        if (hoveringPiece == null || !__instance.CheckCanRemovePiece(hoveringPiece) || !PrivateArea.CheckAccess(hoveringPiece.transform.position))
        {
            return true;
        }


        CraftyCart cart = hoveringPiece.GetComponent<CraftyCart>();
        if (cart == null)
            return true;

        Piece? selectedPiece = __instance.GetSelectedPiece();
        if (selectedPiece == null) return true;
        if (selectedPiece.m_name != "$piece_upgradecart") return true;

        int nextLevel = cart.GetNextLevel();
        if (nextLevel == -1)
        {
            cart.LogUpgrade("<color=white>$msg_maxlevel</color>", cart, __instance, hoveringPiece);
            return false;
        }

        CraftyCart.UpgradeVisualMapping mapping = System.Array.Find(cart.upgradeVisualMappings, x => x.requiredLevel == nextLevel);
        if (mapping == null)
        {
            cart.LogUpgrade("<color=white>$msg_cantupgrade</color>", cart, __instance, hoveringPiece);
            return false;
        }


        Piece.Requirement[]? nextLevelRequirements = cart.GetRequirements(nextLevel);
        if (nextLevelRequirements == null)
        {
            cart.LogUpgrade("<color=white>$msg_cantupgrade</color>", cart, __instance, hoveringPiece);
            return false;
        }

        selectedPiece.m_resources = nextLevelRequirements;
        if (__instance.NoCostCheat() || __instance.HaveRequirements(selectedPiece, Player.RequirementMode.CanBuild))
        {
            cart.RequestUpgrade(nextLevel);

            cart.LogUpgrade("<color=white>$msg_cartupgraded</color>", cart, __instance, hoveringPiece);
            __instance.m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
            hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation);
            if (!__instance.NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(selectedPiece.FreeBuildKey()))
                __instance.ConsumeResources(selectedPiece.m_resources, 0);
        }
        else
        {
            __instance.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
        }


        return false; // Skip the normal repair logic.
    }
}

// Patch the method that updates the hovered piece in the placement/repair mode.
[HarmonyPatch(typeof(Player), nameof(Player.UpdateWearNTearHover))]
public static class UpgradeCartHoverPatch
{
    static void Postfix(Player __instance)
    {
        Piece hovered = __instance.GetHoveringPiece();
        if (hovered == null)
            return;


        CraftyCart cart = hovered.GetComponent<CraftyCart>();
        if (cart == null)
            return;

        int nextLevel = cart.GetNextLevel();
        if (nextLevel == -1)
            return; // Already maxed out.

        Piece.Requirement[]? nextReqs = cart.GetRequirements(nextLevel);
        if (nextReqs == null || nextReqs.Length == 0)
            return;


        Piece selected = __instance.GetSelectedPiece();
        if (selected == null)
            return;

        if (__instance.GetSelectedPiece().m_name == "$piece_upgradecart")
        {
            if (selected.m_resources != null && selected.m_resources == nextReqs)
                return;

            if (!ObjectDB.instance || ObjectDB.instance.GetItemPrefab("YmirRemains") == null) return;
            selected.m_resources = nextReqs;
            foreach (Piece instantiatedPiece in Object.FindObjectsOfType<Piece>())
            {
                if (instantiatedPiece.m_name == selected.m_name)
                {
                    instantiatedPiece.m_resources = nextReqs;
                }
            }
        }
    }
}