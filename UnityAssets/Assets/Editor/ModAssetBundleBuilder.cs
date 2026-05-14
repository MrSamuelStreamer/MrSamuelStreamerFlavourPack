using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds per-platform AssetBundles for MSSFP custom shaders.
/// Output lands in ../Common/AssetBundles/{mssfp_linux,mssfp_mac,mssfp_win}.
/// RimWorld 1.6 auto-loads these and exposes contained shaders via
/// ContentFinder&lt;Shader&gt;.Get("&lt;ShaderName&gt;").
///
/// Menu: Assets > Build MSSFP AssetBundles (LZ4).
/// CLI:
///   Unity -batchmode -projectPath ./UnityAssets \
///         -executeMethod ModAssetBundleBuilder.BuildBundles \
///         --assetBundleName=mssfp -quit
/// </summary>
public class ModAssetBundleBuilder
{
    private const string BundleName = "mssfp";
    private const string OutputDirectoryRoot = "../Common/AssetBundles";

    [MenuItem("Assets/Build MSSFP AssetBundles (LZ4)")]
    public static void BuildBundles()
    {
        // CLI override for CI: --assetBundleName=<name>
        string assetBundleName = BundleName;
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith("--assetBundleName="))
            {
                assetBundleName = arg.Substring("--assetBundleName=".Length);
                Debug.Log($"Using asset bundle name: {assetBundleName}");
            }
        }

        // Auto-label every asset under Assets/Data with our bundle name.
        string[] assetPaths = AssetLabeler.LabelAllAssetsWithCommonName(assetBundleName).ToArray();
        if (assetPaths.Length == 0)
        {
            Debug.LogError("No assets were labeled; aborting asset bundle build.");
            return;
        }

        Debug.Log("Building MSSFP asset bundles...");
        foreach (string assetPath in assetPaths)
        {
            Debug.Log($"  Including: {assetPath}");
        }

        if (!System.IO.Directory.Exists(OutputDirectoryRoot))
            System.IO.Directory.CreateDirectory(OutputDirectoryRoot);

        // LZ4 chunk-based compression — best balance of size + load time for runtime AssetBundles.
        AssetBundleBuild[] bundles = new AssetBundleBuild[1];

        bundles[0] = new AssetBundleBuild
        {
            assetBundleName = assetBundleName + "_linux",
            assetNames = assetPaths
        };
        BuildPipeline.BuildAssetBundles(OutputDirectoryRoot, bundles,
            BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneLinux64);

        bundles[0] = new AssetBundleBuild
        {
            assetBundleName = assetBundleName + "_mac",
            assetNames = assetPaths
        };
        BuildPipeline.BuildAssetBundles(OutputDirectoryRoot, bundles,
            BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneOSX);

        bundles[0] = new AssetBundleBuild
        {
            assetBundleName = assetBundleName + "_win",
            assetNames = assetPaths
        };
        BuildPipeline.BuildAssetBundles(OutputDirectoryRoot, bundles,
            BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);

        Debug.Log("MSSFP asset bundles built successfully to " + OutputDirectoryRoot);
    }
}
