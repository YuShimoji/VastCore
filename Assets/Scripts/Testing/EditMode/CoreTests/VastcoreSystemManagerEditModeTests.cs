using System;
using System.Linq;
using NUnit.Framework;
using Vastcore.Core;

namespace Vastcore.Testing.EditMode.CoreTests
{
    public class VastcoreSystemManagerEditModeTests
    {
        [Test]
        public void SystemStatusEnum_DefinesExpectedLifecycleValues()
        {
            var names = Enum.GetNames(typeof(VastcoreSystemManager.SystemStatus));

            CollectionAssert.AreEquivalent(
                new[] { "NotInitialized", "Initializing", "Running", "Error", "Shutdown" },
                names);
        }

        [Test]
        public void PublicApi_ContainsSystemInfoAccessor()
        {
            var method = typeof(VastcoreSystemManager)
                .GetMethods()
                .FirstOrDefault(m => m.Name == "GetSystemInfo" && m.ReturnType == typeof(string));

            Assert.IsNotNull(method);
        }
    }
}
