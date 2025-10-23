using System;
using System.Linq;
using UnityEngine;

namespace CraftyCartsRemake
{
    public class CraftyCart : MonoBehaviour
    {
        public Vagon m_vagon = null!;
        public ZNetView m_zNetView = null!;
        public Piece m_piece = null!;
        public LineRenderer m_lineRenderer = null!;
        public WearNTear m_wearNTear = null!;
        public ImpactEffect m_impactEffect = null!;
        public GameObject[] m_wheels = null!;
        public Container m_container = null!;
        public AudioSource[] m_wheelaudios = null!;
        public ZSFX[] m_wheelzSFXs = null!;
        public Transform m_lineAttachLeft = null!;
        public Transform m_lineAttachRight = null!;
        public CraftingStation m_craftingStation = null!;
        public SmokeSpawner m_smokeSpawner = null!;
        public CircleProjector m_projector = null!;
        public ParticleSystem m_projectorParticleSystem = null!;
        public GameObject bumperSticker;
        public GameObject upgradeInfo;

        [Header("Upgrade Visuals Settings")] [Tooltip("ZDO key that stores the cart's upgrade level.")]
        public static readonly int upgradeLevelKey = "upgradeLevel".GetStableHashCode();


        [Tooltip("Parent transform to attach upgrade visuals. This container should hold only visuals/VFX.")]
        public Transform upgradeVisualsParent = null!;

        [Tooltip("Maximum upgrade level allowed.")]
        public int maxUpgradeLevel = 1;

        [Header("Upgrade Visuals Toggling")] [Tooltip("Current upgrade level of the cart (for example, retrieved from the ZDO).")]
        public int currentUpgradeLevel = 1;

        [Tooltip("Mappings for each upgrade visual. Each mapping specifies the required level to activate the visual.")]
        public UpgradeVisualMapping[] upgradeVisualMappings = new UpgradeVisualMapping[] { };

        [Serializable]
        public class UpgradeVisualMapping
        {
            [Tooltip("Minimum upgrade level required for this visual to be active.")]
            public int requiredLevel;

            [Tooltip("The GameObject (under the UpgradeVisuals container) that represents this upgrade visual.")]
            public GameObject visualObject;

            [Tooltip("The recipe required to upgrade to this level.")]
            public Piece.Requirement[] m_resources = Array.Empty<Piece.Requirement>();
        }

        private void Awake()
        {
            // Auto-assign the ZNetView if not already set.
            if (!m_zNetView)
                m_zNetView = GetComponent<ZNetView>();

            // Register our RPC so that clients can request an upgrade.
            if (m_zNetView)
            {
                m_zNetView.Register("RPC_SetUpgradeLevel", RPC_SetUpgradeLevel);
            }

            if (CCR.UseBumperSticker.Value.IsOn())
            {
                bumperSticker.gameObject.SetActive(true);
            }

            // Auto-populate the upgradeVisualMappings from the children of upgradeVisualsParent.
            if (upgradeVisualsParent)
            {
                int childCount = upgradeVisualsParent.childCount;
                upgradeVisualMappings = new UpgradeVisualMapping[childCount];

                for (int i = 0; i < childCount; ++i)
                {
                    GameObject child = upgradeVisualsParent.GetChild(i).gameObject;
                    // First child's required level is 2 (since level 1 is the base level), then 3, etc.
                    upgradeVisualMappings[i] = new UpgradeVisualMapping
                    {
                        requiredLevel = i + 2,
                        visualObject = child,
                        m_resources = ZNetScene.instance.GetPrefab(child.name).GetComponent<Piece>().m_resources
                    };
                }
            }
            else
            {
                Debug.LogWarning("upgradeVisualsParent is null. Cannot auto-populate upgrade visual mappings.");
            }

            maxUpgradeLevel = upgradeVisualMappings.Length + 1;


            foreach (var material in transform.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material materialMaterial in material.materials)
                {
                    if (materialMaterial.shader.name == "Custom/Piece")
                    {
                        materialMaterial.SetFloat("_ValueNoise", 0f);
                    }
                }
            }

            // Add an event to the wearntears's OnDestroyed event. wearNtear.m_onDestroyed += new Action(this.OnDestroyed);
            m_wearNTear.m_onDestroyed += OnDestroyed;
        }

        private void Start()
        {
            if (m_zNetView == null || m_zNetView.GetZDO() == null)
                return;

            currentUpgradeLevel = m_zNetView.GetZDO().GetInt(upgradeLevelKey, 1);
            UpdateUpgradeVisuals();
        }

        private void OnDestroy()
        {
            m_wearNTear.m_onDestroyed -= OnDestroyed;
        }

        private void OnDestroyed()
        {
            if (!m_zNetView.IsOwner())
                return;
            if (!m_container) return;
            
            foreach (UpgradeVisualMapping upgradeVisualMapping in upgradeVisualMappings.Where(x => x.requiredLevel <= currentUpgradeLevel))
            {
                foreach (Piece.Requirement requirement in upgradeVisualMapping.m_resources)
                {
                    m_container.GetInventory().AddItem(requirement.m_resItem.m_itemData.m_dropPrefab, requirement.m_amount);
                }
            }
        }

        /// <summary>
        /// Call this method when the cart's upgrade level changes.
        /// This event‑driven method updates the visuals accordingly.
        /// </summary>
        /// <param name="newLevel">The new upgrade level.</param>
        public void SetUpgradeLevel(int newLevel)
        {
            if (newLevel == currentUpgradeLevel)
                return;

            currentUpgradeLevel = newLevel;

            // Optionally update the ZDO to reflect the new level.
            if (m_zNetView && m_zNetView.GetZDO() != null)
            {
                m_zNetView.GetZDO().Set(upgradeLevelKey, currentUpgradeLevel);
            }

            UpdateUpgradeVisuals();
        }

        /// <summary>
        /// Call this method when the cart's upgrade level changes.
        /// This method toggles each visual on or off based on the current level.
        /// </summary>
        public void UpdateUpgradeVisuals()
        {
            if (upgradeVisualMappings == null)
                return;
            if (upgradeVisualMappings.Length == 0)
                return;
            foreach (UpgradeVisualMapping mapping in upgradeVisualMappings)
            {
                if (mapping.visualObject)
                {
                    // Activate if the current upgrade level is equal to or above the required level.
                    mapping.visualObject.SetActive(currentUpgradeLevel >= mapping.requiredLevel);
                }
            }
        }

        /// <summary>
        /// RPC method invoked over the network to set the upgrade level.
        /// Expected signature: void RPC_SetUpgradeLevel(long sender)
        /// </summary>
        /// <param name="sender">The sender's network ID.</param>
        private void RPC_SetUpgradeLevel(long sender)
        {
            // Only process if we are the owner.
            if (!m_zNetView.IsOwner())
                return;

            // For simplicity, we just increment the level by one.
            SetUpgradeLevel(currentUpgradeLevel + 1);
        }

        /// <summary>
        /// Called on a client when a player wants to upgrade the cart.
        /// This method sends an RPC to the owner so that the upgrade can be processed.
        /// </summary>
        /// <param name="newLevel">The desired new upgrade level.</param>
        public void RequestUpgrade(int newLevel)
        {
            if (!m_zNetView)
                return;

            // If we're not the owner, send an RPC to request the upgrade.
            if (!m_zNetView.IsOwner())
            {
                m_zNetView.InvokeRPC("RPC_SetUpgradeLevel", newLevel);
            }
            else
            {
                // If we are the owner, update directly.
                SetUpgradeLevel(newLevel);
            }
        }

        /// <summary>
        /// Returns the next upgrade level.
        /// If the cart is at max level, returns -1.
        /// </summary>
        public int GetNextLevel()
        {
            return (currentUpgradeLevel < maxUpgradeLevel) ? currentUpgradeLevel + 1 : -1;
        }

        /// <summary>
        /// Returns the recipe name for the specified upgrade level.
        /// The index is calculated as (level - 2) since base level is 1.
        /// Returns null if not found.
        /// </summary>
        public Piece.Requirement[]? GetRequirements(int level)
        {
            return upgradeVisualMappings.Where(x => x.requiredLevel == level).Select(x => x.m_resources).FirstOrDefault();
        }

        public void LogUpgrade(string message, CraftyCart cart, Player playerInstance, Piece hoveringPiece)
        {
            Chat.instance.AddInworldText(
                upgradeInfo,
                playerInstance.GetPlayerID(),
                upgradeInfo.transform.position,
                Talker.Type.Normal,
                UserInfo.GetLocalUser(),
                Localization.instance.Localize(message, Localization.instance.Localize(hoveringPiece.m_name))
            );
        }
    }
}