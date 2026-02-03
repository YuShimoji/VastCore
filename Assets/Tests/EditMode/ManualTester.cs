// Disabled: StructureGeneratorWindow and StructureGenerationTab not yet implemented
#if VASTCORE_STRUCTURE_GENERATOR_ENABLED
using UnityEditor;
using UnityEngine;
using Vastcore.Editor.Generation;
using System.Reflection;

namespace Vastcore.Editor.Testing
{
    public class ManualTester
    {
        [MenuItem("Test/1. Generate Cube")]
        public static void RunCubeGenerationTest()
        {
            // StructureGeneratorWindowは実際には開かれないが、
            // コンストラクタ引数のためにインスタンスが必要
            var window = ScriptableObject.CreateInstance<StructureGeneratorWindow>();
            var generationTab = new StructureGenerationTab(window);

            // privateフィールド 'cubeSize' に値を設定
            FieldInfo cubeSizeField = typeof(StructureGenerationTab).GetField("cubeSize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cubeSizeField != null)
            {
                cubeSizeField.SetValue(generationTab, 5.0f);
            }
            else
            {
                Debug.LogError("'cubeSize' field not found.");
                return;
            }

            // privateメソッド 'GenerateCube' を呼び出し
            MethodInfo generateCubeMethod = typeof(StructureGenerationTab).GetMethod("GenerateCube", BindingFlags.NonPublic | BindingFlags.Instance);
            if (generateCubeMethod != null)
            {
                generateCubeMethod.Invoke(generationTab, null);
                Debug.Log("Test Executed: Cube generation attempted.");
            }
            else
            {
                Debug.LogError("'GenerateCube' method not found.");
            }
            
            // ScriptableObjectのインスタンスは不要になったら破棄
            Object.DestroyImmediate(window);
        }
    }
}
#endif
