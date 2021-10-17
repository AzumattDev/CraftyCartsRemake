using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CraftyCartsRemake.Utilities
{
    internal static class ResourceUtils
    {
        public static AssetBundle GetAssetBundle(string filename)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using var stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }
    }
}