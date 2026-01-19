using UnityEngine;
using UnityEditor;
using Vastcore.Generation.Optimization;

namespace Vastcore.Editor.Inspectors
{
    [CustomEditor(typeof(PerformanceOptimizationController))]
    public class PerformanceOptimizationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var controller = (PerformanceOptimizationController)target;

            GUILayout.Space(10);
            GUILayout.Label("Debug Metrics", EditorStyles.boldLabel);

            var metrics = controller.GetCurrentMetrics();
            var state = controller.GetOptimizationState();

            // Status Display
            Color originalColor = GUI.color;
            switch (state)
            {
                case PerformanceOptimizationController.OptimizationState.Optimal:
                    GUI.color = Color.green;
                    break;
                case PerformanceOptimizationController.OptimizationState.Degraded:
                    GUI.color = Color.yellow;
                    break;
                case PerformanceOptimizationController.OptimizationState.Critical:
                    GUI.color = Color.red;
                    break;
                case PerformanceOptimizationController.OptimizationState.Recovery:
                    GUI.color = Color.cyan;
                    break;
            }
            GUILayout.Label($"State: {state}", EditorStyles.boldLabel);
            GUI.color = originalColor;

            // Metrics Display
            EditorGUILayout.LabelField("FPS", $"{metrics.frameRate:F1}");
            EditorGUILayout.LabelField("Frame Time", $"{metrics.frameTime:F2} ms");
            EditorGUILayout.LabelField("GPU Memory", $"{metrics.gpuMemoryUsage:F1} MB");
            EditorGUILayout.LabelField("Cache Hit Ratio", $"{metrics.cacheHitRatio:F2}");
            EditorGUILayout.LabelField("Active Generations", $"{metrics.activeGenerations}");

            GUILayout.Space(10);

            if (GUILayout.Button("Force Optimization"))
            {
                controller.ForceOptimization();
            }

            if (GUILayout.Button("Reset Settings"))
            {
                controller.ResetOptimizationSettings();
            }
        }

        // Ideally we also want to repaint if the game is running to show live stats
        // but OnInspectorGUI only updates on interaction usually.
        // We can force update if in PlayMode
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
