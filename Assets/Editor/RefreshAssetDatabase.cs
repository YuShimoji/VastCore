using UnityEditor;
using UnityEngine;

public class RefreshAssetDatabase : MonoBehaviour
{
    [MenuItem("Tools/Refresh Asset Database")]
    public static void Refresh()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("Asset Database refreshed!");
    }
}
