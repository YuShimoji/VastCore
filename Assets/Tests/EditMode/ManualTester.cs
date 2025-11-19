using UnityEditor;
using UnityEngine;

namespace Vastcore.Editor.Testing
{
    public class ManualTester
    {
        [MenuItem("Test/1. Generate Cube")]
        public static void RunCubeGenerationTest()
        {
            // 旧 StructureGeneratorWindow 依存を外したプレースホルダー実装
            Debug.Log("RunCubeGenerationTest is disabled in this build (StructureGeneratorWindow not available).");
        }
    }
}
