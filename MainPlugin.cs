using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CraftyCartsRemake.Utilities;
using HarmonyLib;
using UnityEngine;

namespace CraftyCartsRemake
{
    [BepInPlugin("com.rolopogo.CraftyCarts", "CraftyCarts", "2.1.0")]
    public class CCR : BaseUnityPlugin
    {
        private static CCR instance;
        private static GameObject? _prefabRoot;
        private static Action? _onAfterInit;
        private ConfigEntry<bool>? _enabledConfig;
        private static ConfigEntry<float>? _cartweight;
        public static ManualLogSource? Log { get; private set; }

        public void Awake()
        {
            instance = this;
            Log = Logger;
            _enabledConfig = Config.Bind("General", "Enabled", true, "Enables CraftyCarts");
            _cartweight = Config.Bind("General", "Cart Weight", 0.3f, "Modifies the weight of the carts by changing the center of mass on the RigidBody of the object. Default formula is Vector3.up * 0.3f.");

            if (!_enabledConfig.Value) return;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "rolopogo.CraftyCarts");
            _onAfterInit += AddPieceToTool;
        }

        private static void AddPieceToTool()
        {
            if (_prefabRoot) return;

            MaterialReplacer.GetAllMaterials();

            _prefabRoot = new GameObject("CartPrefabs");
            _prefabRoot.SetActive(false);
            DontDestroyOnLoad(_prefabRoot);

            var cart = ZNetScene.instance.GetPrefab("Cart");

            var workbenchCart = Instantiate(cart, _prefabRoot.transform);
            DestroyImmediate(workbenchCart.transform.Find("AttachPoint").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("Wheel1").GetChild(0).gameObject);
            DestroyImmediate(workbenchCart.transform.Find("Wheel2").GetChild(0).gameObject);
            DestroyImmediate(workbenchCart.transform.Find("Container").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("Vagon").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("load").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("LineAttach0").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("LineAttach1").gameObject);
            DestroyImmediate(workbenchCart.transform.Find("cart_Destruction").gameObject);

            cart.GetComponent<Rigidbody>().mass = 50;

            workbenchCart.name = "CraftyCarts.WorkbenchCart";

            var stoneCart = Instantiate(workbenchCart, _prefabRoot.transform);
            stoneCart.name = "CraftyCarts.StoneCart";
            var forgeCart = Instantiate(workbenchCart, _prefabRoot.transform);
            forgeCart.name = "CraftyCarts.ForgeCart";

            // Load from assetbundle
            /*var bundle = AssetBundle.LoadFromMemory(ResourceUtils.GetResource(Assembly.GetExecutingAssembly(),
                "CraftyCarts.Resources.carts"));*/
            var bundle = ResourceUtils.GetAssetBundle("carts");
            var workbenchCartModel = bundle.LoadAsset<GameObject>("workbench_cart");
            var wbIcon = bundle.LoadAsset<Sprite>("workbenchcarticon");
            var forgeCartModel = bundle.LoadAsset<GameObject>("forge_cart");
            var fIcon = bundle.LoadAsset<Sprite>("forgecarticon");
            var stoneCartModel = bundle.LoadAsset<GameObject>("stone_cart");
            var scIcon = bundle.LoadAsset<Sprite>("stonecarticon");

            bundle.Unload(false);

            Debug.Log("add crafting station");
            var workbench = ZNetScene.instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
            var stoneCutter = ZNetScene.instance.GetPrefab("piece_stonecutter").GetComponent<CraftingStation>();
            var forge = ZNetScene.instance.GetPrefab("forge").GetComponent<CraftingStation>();

            var wbPiece = SetupCraftingStation(workbenchCartModel, workbench, workbenchCart);
            var fPiece = SetupCraftingStation(forgeCartModel, forge, forgeCart);
            var scPiece = SetupCraftingStation(stoneCartModel, stoneCutter, stoneCart);

            wbPiece.m_icon = wbIcon;
            fPiece.m_icon = fIcon;
            scPiece.m_icon = scIcon;


            // Add to tool
            var hammerPrefab = ObjectDB.instance.GetItemPrefab("Hammer");
            var hammerPieceTable = hammerPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;

            if (hammerPieceTable.m_pieces.Contains(workbenchCart.gameObject)) return;
            hammerPieceTable.m_pieces.Add(workbenchCart.gameObject);
            hammerPieceTable.m_pieces.Add(forgeCart.gameObject);
            hammerPieceTable.m_pieces.Add(stoneCart.gameObject);
            var zNetScene = ZNetScene.instance;
            if (!zNetScene) return;
            zNetScene.m_namedPrefabs.Add(workbenchCart.name.GetStableHashCode(), workbenchCart);

            zNetScene.m_namedPrefabs.Add(forgeCart.name.GetStableHashCode(), forgeCart);
            zNetScene.m_namedPrefabs.Add(stoneCart.name.GetStableHashCode(), stoneCart);
        }

        private static Piece SetupCraftingStation(GameObject model, CraftingStation station, GameObject cart)
        {
            //Add model to cart
            var modelInstance = Instantiate(model, cart.transform);
            modelInstance.SetActive(true);

            // update material references
            MaterialReplacer.ReplaceAllMaterialsWithOriginal(modelInstance);

            Debug.Log("fix component refs");
            //Fix component refs
            var wbWNT = cart.GetComponent<WearNTear>();
            wbWNT.m_new = modelInstance.transform.Find("new").gameObject;
            wbWNT.m_worn = modelInstance.transform.Find("worn").gameObject;
            wbWNT.m_broken = modelInstance.transform.Find("broken").gameObject;
            wbWNT.m_fragmentRoots = new[] { modelInstance.transform.Find("cart_Destruction").gameObject };

            var rb = cart.GetComponent<Rigidbody>();
            rb.centerOfMass = Vector3.up * _cartweight.Value;

            Debug.Log("fix vagon");
            var vagon = cart.GetComponent<Vagon>();
            vagon.m_name = station.m_name + " $tool_cart";
            vagon.m_attachPoint = modelInstance.transform.Find("AttachPoint");
            vagon.m_lineAttachPoints0 = modelInstance.transform.Find("LineAttach0");
            vagon.m_lineAttachPoints1 = modelInstance.transform.Find("LineAttach1");
            vagon.m_container = null;
            vagon.m_loadVis = new List<Vagon.LoadData>();

            Debug.Log("fix wheels");
            cart.transform.Find("Wheel1").position = modelInstance.transform.Find("Wheel1").position;
            cart.transform.Find("Wheel2").position = modelInstance.transform.Find("Wheel2").position;
            modelInstance.transform.Find("Wheel1").SetParent(cart.transform.Find("Wheel1"), false);
            modelInstance.transform.Find("Wheel2").SetParent(cart.transform.Find("Wheel2"), false);
            cart.transform.Find("Wheel1/Wheel1").localRotation = Quaternion.identity;
            cart.transform.Find("Wheel2/Wheel2").localRotation = Quaternion.identity;
            cart.transform.Find("Wheel1/Wheel1").localPosition = Vector3.zero;
            cart.transform.Find("Wheel2/Wheel2").localPosition = Vector3.zero;

            Debug.Log("add crafting station");
            var cs = modelInstance.transform.Find("crafting station").gameObject.AddComponent<CraftingStation>();
            cs.m_name = station.m_name;
            cs.m_icon = station.m_icon;
            cs.m_discoverRange = station.m_discoverRange;
            cs.m_rangeBuild = station.m_rangeBuild;
            cs.m_craftRequireRoof = false;
            cs.m_craftRequireFire = false;
            var transform1 = cs.transform;
            cs.m_roofCheckPoint = transform1;
            cs.m_connectionPoint = transform1;
            cs.m_showBasicRecipies = station.m_showBasicRecipies;
            cs.m_useDistance = station.m_useDistance;
            cs.m_useAnimation = station.m_useAnimation;
            cs.m_craftItemEffects = station.m_craftItemEffects;
            cs.m_craftItemDoneEffects = station.m_craftItemDoneEffects;
            cs.m_repairItemDoneEffects = station.m_repairItemDoneEffects;
            if (cs.transform.Find("AreaMarker"))
                DestroyImmediate(cs.transform.Find("AreaMarker"));
            cs.m_areaMarker = Instantiate(station.transform.Find("AreaMarker"), modelInstance.transform).gameObject;
            cs.gameObject.name = "CraftyCarts.CraftingStation";


            Debug.Log("setup recipe");
            var piece = cart.GetComponent<Piece>();
            piece.m_name = station.m_name + " $tool_cart";

            piece.m_craftingStation = station.GetComponent<Piece>().m_craftingStation;

            // Recipes
            var nail = new Piece.Requirement
            {
                m_recover = true,
                m_amount = 10,
                m_resItem = ObjectDB.instance.GetItemPrefab("BronzeNails").GetComponent<ItemDrop>()
            };

            var newReqs = station.GetComponent<Piece>().m_resources.ToList();
            newReqs.Add(nail);
            piece.m_resources = newReqs.ToArray();
            return piece;
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class CcrObjectDBAwakePatch
        {
            public static void Postfix(ObjectDB __instance)
            {
                if (__instance == null || __instance.m_items.Count == 0 ||
                    __instance.GetItemPrefab("Amber") == null) return;
                _onAfterInit.SafeInvoke();
                _onAfterInit = null;
            }
        }
    }

    public static class Events
    {
        public static void SafeInvoke(this Action? events)
        {
            if (events == null) return;

            foreach (var @delegate in events.GetInvocationList())
            {
                var @event = (Action)@delegate;
                try
                {
                    @event();
                }
                catch (Exception e)
                {
                    CCR.Log?.LogError(e);
                }
            }
        }
    }
}