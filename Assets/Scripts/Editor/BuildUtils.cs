// Copyright (c) Meta Platforms, Inc. and affiliates.    

using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildUtils
{
    [MenuItem("AssetStreaming/Build Addressables and Apk")]
    public static void BuildAddressablesAndApk()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Can't build apk, swicth the platform to Android");
            return;
        }
        BuildPlayerOptions buildPlayerOptions = 
            BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        if (BuildAddressables())
        {
            BuildApk(buildPlayerOptions);
        }
    }
    
    [MenuItem("AssetStreaming/Build Addressables only")]
    public static bool BuildAddressables()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);
        if (!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
            return false;
        }

        return true;
    }
    
    [MenuItem("AssetStreaming/Build Apk only")]
    public static bool BuildApk()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Can't build apk, swicth the platform to Android");
            return false;
        }
        BuildPlayerOptions buildPlayerOptions = 
            BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        return BuildApk(buildPlayerOptions);
    }

    [MenuItem("AssetStreaming/Build Addressables and Apk", true)]
    [MenuItem("AssetStreaming/Build Addressables", true)]
    [MenuItem("AssetStreaming/Build Apk", true)]
    public static bool MenuValidation()
    {
        return !Application.isPlaying;
    }

    private static bool BuildApk(BuildPlayerOptions buildPlayerOptions)
    {
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log($"Build apk in {report.summary.totalTime.TotalSeconds} sec");
        return report.summary.result == BuildResult.Succeeded;
    }
}
