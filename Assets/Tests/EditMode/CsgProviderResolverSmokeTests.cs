using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    public sealed class CsgProviderResolverSmokeTests
    {
        [Test]
        public void TryExecuteWithFallback_NullInputs_ReturnsFalseAndProvidesError()
        {
            var resolverType = Type.GetType(
                "Vastcore.Editor.Generation.Csg.CsgProviderResolver, Vastcore.Editor.StructureGenerator",
                throwOnError: false);
            Assert.IsNotNull(resolverType);

            var operationType = Type.GetType(
                "Vastcore.Editor.Generation.Csg.CsgOperation, Vastcore.Editor.StructureGenerator",
                throwOnError: false);
            Assert.IsNotNull(operationType);

            var method = resolverType!.GetMethod(
                "TryExecuteWithFallback",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);

            var unionOperation = Enum.Parse(operationType!, "Union");
            var args = new object[]
            {
                null,
                null,
                unionOperation,
                null,
                null,
                null,
                null
            };

            var ok = (bool)method!.Invoke(null, args)!;
            var mesh = args[3] as Mesh;
            var materials = args[4] as Material[];
            var providerName = args[5] as string;
            var error = args[6] as string;

            Assert.IsFalse(ok);
            Assert.IsNull(mesh);
            Assert.IsNotNull(materials);
            Assert.IsTrue(string.IsNullOrWhiteSpace(providerName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }
    }
}
