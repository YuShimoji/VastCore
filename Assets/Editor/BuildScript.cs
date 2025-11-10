using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/Main.unity" };
        BuildPipeline.BuildPlayer(scenes, "Build/VastCore.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}
