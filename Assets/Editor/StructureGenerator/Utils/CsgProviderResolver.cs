using System;
using UnityEngine;

namespace Vastcore.Editor.Generation.Csg
{
    internal static class CsgProviderResolver
    {
        private static readonly ICsgProvider[] Providers =
        {
            new ProBuilderInternalCsgProvider(),
            new ParaboxCsgProvider()
        };

        public static bool TryExecuteWithFallback(GameObject lhs, GameObject rhs, CsgOperation operation, out Mesh mesh, out Material[] materials, out string providerName, out string error)
        {
            mesh = null;
            materials = Array.Empty<Material>();
            providerName = string.Empty;
            error = string.Empty;

            foreach (var provider in Providers)
            {
                if (!provider.IsAvailable(out _))
                {
                    continue;
                }

                if (provider.TryExecute(lhs, rhs, operation, out mesh, out materials, out error))
                {
                    providerName = provider.Name;
                    return true;
                }
            }

            error = string.IsNullOrWhiteSpace(error)
                ? "No available CSG provider succeeded."
                : error;

            return false;
        }
    }
}
