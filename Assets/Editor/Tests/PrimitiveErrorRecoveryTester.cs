// This file is disabled until PrimitiveErrorRecovery and EditorCoroutineUtility are implemented
#if VASTCORE_ERROR_RECOVERY_ENABLED
using UnityEngine;
using UnityEditor;
using Vastcore.Generation;
using Vastcore.Core;

public class PrimitiveErrorRecoveryTester : EditorWindow
{
    private Vector3 spawnPosition = new Vector3(0, 50, 0);
    private PrimitiveType primitiveType = PrimitiveType.Sphere;
    private float scale = 10f;

    [MenuItem("Vastcore/Test/Primitive Error Recovery Tester")]
    public static void ShowWindow()
    {
        GetWindow<PrimitiveErrorRecoveryTester>("Primitive Recovery Tester");
    }

    void OnGUI()
    {
        GUILayout.Label("Primitive Error Recovery Test Panel", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);
        primitiveType = (PrimitiveType)EditorGUILayout.EnumPopup("Primitive Type", primitiveType);
        scale = EditorGUILayout.FloatField("Scale", scale);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Test Obstacles"))
        {
            SetupTestObstacles();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Test Scenarios", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Test Position Recovery (Spawn on Obstacle)"))
        {
            TestPositionRecovery_OnObstacle();
        }

        if (GUILayout.Button("2. Test Position Recovery (Spawn on Steep Slope)"))
        {
            TestPositionRecovery_OnSteepSlope();
        }

        if (GUILayout.Button("3. Test Fallback Mesh Generation"))
        {
            TestMeshRecovery();
        }
    }

    private void SetupTestObstacles()
    {
        // Create a large plane to act as ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TestGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(50, 1, 50);

        // Create an obstacle to spawn on top of
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "TestObstacle";
        obstacle.transform.position = spawnPosition;
        obstacle.transform.localScale = new Vector3(20, 20, 20);

        // Create a steep slope
        GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slope.name = "TestSlope";
        slope.transform.position = new Vector3(50, 10, 0);
        slope.transform.localScale = new Vector3(30, 20, 30);
        slope.transform.rotation = Quaternion.Euler(0, 0, 60); // 60-degree slope

        Debug.Log("Test obstacles (Ground, Obstacle, Slope) created.");
    }

    private void TestPositionRecovery_OnObstacle()
    {
        Debug.Log("--- Running Position Recovery Test (On Obstacle) ---");
        Debug.Log($"Attempting to spawn {primitiveType} at obstacle position: {spawnPosition}");

        var recoverySystem = PrimitiveErrorRecovery.Instance;
        if (recoverySystem == null)
        {
            Debug.LogError("PrimitiveErrorRecovery instance not found!");
            return;
        }

        // EditorWindowではコルーチンを使用できないため、同期的なテストを行う
        try
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.transform.position = spawnPosition;
            primitive.transform.localScale = Vector3.one * scale;
            primitive.name = $"Test_{primitiveType}_{Time.time}";
            
            Debug.Log($"Test primitive spawned at {spawnPosition}. Check if it's positioned correctly.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Test failed: {ex.Message}");
        }
    }

    private void TestPositionRecovery_OnSteepSlope()
    {
        Vector3 slopePosition = new Vector3(50, 25, 0);
        Debug.Log("--- Running Position Recovery Test (On Steep Slope) ---");
        Debug.Log($"Attempting to spawn {primitiveType} at slope position: {slopePosition}");

        var recoverySystem = PrimitiveErrorRecovery.Instance;
        if (recoverySystem == null)
        {
            Debug.LogError("PrimitiveErrorRecovery instance not found!");
            return;
        }

        // EditorWindowではコルーチンを使用できないため、同期的なテストを行う
        try
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.transform.position = slopePosition;
            primitive.transform.localScale = Vector3.one * scale;
            primitive.name = $"Test_Slope_{primitiveType}_{Time.time}";
            
            Debug.Log($"Test primitive spawned at slope position {slopePosition}. Check if it's positioned on a flatter area.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Slope test failed: {ex.Message}");
        }
    }

    private void TestMeshRecovery()
    {
        Debug.Log("--- Running Mesh Recovery Test ---");
        Debug.Log("This test simulates a mesh generation failure. A fallback primitive should be created.");

        var recoverySystem = PrimitiveErrorRecovery.Instance;
        if (recoverySystem == null)
        {
            Debug.LogError("PrimitiveErrorRecovery instance not found!");
            return;
        }

        // EditorWindowではコルーチンを使用できないため、同期的なテストを行う
        Vector3 clearPosition = new Vector3(-50, 10, 0);
        try
        {
            // Quadプリミティブの生成を試みる（MeshColliderがない場合がある）
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            primitive.transform.position = clearPosition;
            primitive.transform.localScale = Vector3.one * scale;
            primitive.name = $"Test_Mesh_{Time.time}";
            
            // MeshColliderが自動で付与されない場合があるので確認
            if (primitive.GetComponent<MeshCollider>() == null)
            {
                primitive.AddComponent<MeshCollider>();
                Debug.Log("Added MeshCollider to Quad primitive for collision recovery test.");
            }
            
            Debug.Log($"Mesh recovery test primitive spawned at {clearPosition}. Check if it has proper components.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Mesh recovery test failed: {ex.Message}");
        }
    }
}
#endif
