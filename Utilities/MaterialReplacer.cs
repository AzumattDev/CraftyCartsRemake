using System.Collections.Generic;
using UnityEngine;

namespace CraftyCartsRemake.Utilities
{
    internal static class MaterialReplacer
    {
        private static Dictionary<string, Material> originalMaterials;

        public static void GetAllMaterials()
        {
            var allmats = Resources.FindObjectsOfTypeAll<Material>();
            originalMaterials = new Dictionary<string, Material>();
            foreach (var item in allmats) originalMaterials[item.name] = item;
        }

        public static void ReplaceAllMaterialsWithOriginal(GameObject go)
        {
            if (originalMaterials == null) GetAllMaterials();

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                foreach (var t in renderer.materials)
                    if (t.name.StartsWith("_REPLACE_"))
                    {
                        var matName = renderer.material.name.Replace(" (Instance)", string.Empty)
                            .Replace("_REPLACE_", "");

                        if (originalMaterials.ContainsKey(matName))
                        {
                            renderer.material = originalMaterials[matName];
                        }
                        else
                        {
                            CCR.Log.LogInfo("No suitable material found to replace: " + matName);
                            // Skip over this material in future
                            originalMaterials[matName] = renderer.material;
                        }
                    }
        }
    }
}