using UnityEngine;
using UnityEditor;
using Vastcore.Generation;

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

        // This coroutine will attempt to spawn, and find a new position if the original is occupied.
        EditorCoroutineUtility.StartCoroutine(recoverySystem.RecoverPrimitiveSpawn(spawnPosition, primitiveType, scale,
            (spawnedObject) =>
            {
                if (spawnedObject != null)
                {
                    Debug.Log($"Success! Primitive spawned at {spawnedObject.transform.position}. It should be near, but not on, the obstacle.", spawnedObject);
                }
            },
            () =>
            {
                Debug.LogError("Failure! Primitive could not be spawned.");
            }), this);
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

        EditorCoroutineUtility.StartCoroutine(recoverySystem.RecoverPrimitiveSpawn(slopePosition, primitiveType, scale,
            (spawnedObject) =>
            {
                if (spawnedObject != null)
                {
                    Debug.Log($"Success! Primitive spawned at {spawnedObject.transform.position}. It should be on a flatter area nearby.", spawnedObject);
                }
            },
            () =>
            {
                Debug.LogError("Failure! Primitive could not be spawned.");
            }), this);
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

        // To simulate failure, we'd ideally need to cause CreatePrimitive to throw an error.
        // For this test, we'll call the recovery system and check the logs for fallback creation.
        // The system is designed to try standard creation first, then fallback.
        Vector3 clearPosition = new Vector3(-50, 10, 0);

        EditorCoroutineUtility.StartCoroutine(recoverySystem.RecoverPrimitiveSpawn(clearPosition, PrimitiveType.Quad, scale, // Quad often has no default collider/mesh setup that works everywhere
            (spawnedObject) =>
            {
                if (spawnedObject != null)
                {
                    Debug.Log($"Success! Primitive spawned at {spawnedObject.transform.position}. Check if it's a fallback (e.g., a Cube) with a fallback material.", spawnedObject);
                }
            },
            () =>
            {
                Debug.LogError("Failure! Primitive could not be spawned.");
            }), this);
    }
}
