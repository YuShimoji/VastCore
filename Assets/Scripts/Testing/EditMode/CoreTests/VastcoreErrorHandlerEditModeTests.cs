using System;
using System.Linq;
using NUnit.Framework;
using Vastcore.Core;

namespace Vastcore.Testing.EditMode.CoreTests
{
    public class VastcoreErrorHandlerEditModeTests
    {
        [Test]
        public void ErrorSeverityEnum_ContainsExpectedValues()
        {
            var names = Enum.GetNames(typeof(VastcoreErrorHandler.ErrorSeverity));

            CollectionAssert.AreEqual(new[] { "Low", "Medium", "High", "Critical" }, names);
        }

        [Test]
        public void PublicApi_ContainsResetAndStatsMethods()
        {
            var methods = typeof(VastcoreErrorHandler).GetMethods().Select(m => m.Name).ToArray();

            CollectionAssert.Contains(methods, "ResetErrorHandler");
            CollectionAssert.Contains(methods, "GetErrorStatistics");
            CollectionAssert.Contains(methods, "GetRecentErrors");
        }
    }
}
