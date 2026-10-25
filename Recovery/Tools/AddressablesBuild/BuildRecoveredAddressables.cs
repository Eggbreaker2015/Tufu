using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

// Runs only in the independent build workspace with the official SDK's editor
// serialization build. Runtime projects keep the player-only SDK assemblies.
public static class BuildRecoveredAddressables
{
    [Serializable] private class Entry
    {
        public string originalInternalId;
        public string exportedPath;
        public string bundle;
        public string[] keys;
        public bool serializedTreeMatches;
    }
    [Serializable] private class Mapping {public bool passed;public int originalAssetCount;public Entry[] mapping;}
    [Serializable] private class BundleInfo {public string name;public string hash;public uint crc;public long bytes;public string[] dependencies;}
    [Serializable] private class Report
    {
        public bool passed;
        public string exception;
        public string target="StandaloneWindows64";
        public int assetCount;
        public int assetKeyCount;
        public string output;
        public List<BundleInfo> bundles=new List<BundleInfo>();
    }

    public static void Run()
    {
        var report=new Report();
        string recovery=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        try
        {
            var mapping=JsonUtility.FromJson<Mapping>(File.ReadAllText(Path.Combine(recovery,"Evidence/addressables-recovered-asset-map.json")));
            if(!mapping.passed||mapping.mapping.Length!=mapping.originalAssetCount||mapping.mapping.Any(e=>!e.serializedTreeMatches))
                throw new InvalidDataException("Original catalog mapping has not passed serialized hierarchy comparison");
            foreach(var entry in mapping.mapping)
                if(AssetDatabase.LoadAssetAtPath<GameObject>(entry.exportedPath)==null)
                    throw new InvalidDataException("Missing recovered prefab: "+entry.exportedPath);

            string output=Path.Combine(Application.streamingAssetsPath,"aa");
            string bundleOutput=Path.Combine(output,"StandaloneWindows64");
            Directory.CreateDirectory(bundleOutput);
            var groups=mapping.mapping.GroupBy(e=>e.bundle).OrderBy(g=>g.Key).ToArray();
            var bundleNames=groups.Select((g,i)=>new {g.Key,Name="recovered_"+i+".bundle"}).ToDictionary(x=>x.Key,x=>x.Name);
            var builds=groups.Select(g=>new AssetBundleBuild
            {
                assetBundleName=bundleNames[g.Key],
                assetNames=g.Select(e=>e.exportedPath).ToArray(),
                addressableNames=g.Select(e=>e.originalInternalId).ToArray()
            }).ToArray();
            var manifest=BuildPipeline.BuildAssetBundles(bundleOutput,builds,
                BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle|BuildAssetBundleOptions.DisableLoadAssetByFileName|BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension,
                BuildTarget.StandaloneWindows64);
            if(manifest==null)throw new InvalidOperationException("Unity did not produce an AssetBundleManifest");

            var entries=new List<ContentCatalogDataEntry>();
            foreach(string name in manifest.GetAllAssetBundles())
            {
                string file=Path.Combine(bundleOutput,name);
                if(!BuildPipeline.GetCRCForAssetBundle(file,out uint crc))throw new InvalidDataException("Could not get bundle CRC: "+file);
                string hash=manifest.GetAssetBundleHash(name).ToString();
                string[] dependencies=manifest.GetAllDependencies(name);
                long bytes=new FileInfo(file).Length;
                var options=new AssetBundleRequestOptions {BundleName=name,Hash=hash,Crc=crc,BundleSize=bytes,UseCrcForCachedBundle=true};
                entries.Add(new ContentCatalogDataEntry(typeof(IAssetBundleResource),
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64/"+name,
                    typeof(AssetBundleProvider).FullName,new object[]{name},dependencies.Cast<object>(),options));
                report.bundles.Add(new BundleInfo {name=name,hash=hash,crc=crc,bytes=bytes,dependencies=dependencies});
            }
            foreach(var entry in mapping.mapping)
                entries.Add(new ContentCatalogDataEntry(typeof(GameObject),entry.originalInternalId,
                    typeof(BundledAssetProvider).FullName,entry.keys.Cast<object>(),new object[]{bundleNames[entry.bundle]}));
            var catalog=new ContentCatalogData(entries,"RecoveredOriginalWindows")
            {
                InstanceProviderData=ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>(),
                SceneProviderData=ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>(),
                ResourceProviderData=new List<ObjectInitializationData>
                {
                    ObjectInitializationData.CreateSerializedInitializationData<AssetBundleProvider>(),
                    ObjectInitializationData.CreateSerializedInitializationData<BundledAssetProvider>()
                }
            };
            byte[] catalogBytes=catalog.SerializeToByteArray();
            string catalogHash=Hash128.Compute(catalogBytes).ToString();
            File.WriteAllBytes(Path.Combine(output,"catalog.bin"),catalogBytes);
            File.WriteAllText(Path.Combine(output,"catalog.hash"),catalogHash);
            var runtime=new ResourceManagerRuntimeData
            {
                BuildTarget=BuildTarget.StandaloneWindows64.ToString(),SettingsHash=catalogHash,
                AddressablesVersion="2.7.6",LogResourceManagerExceptions=true,
                MaxConcurrentWebRequests=3,DisableCatalogUpdateOnStartup=false,IsLocalCatalogInBundle=false
            };
            runtime.CatalogLocations.Add(new ResourceLocationData(new[]{ResourceManagerRuntimeData.kCatalogAddress},
                "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin",typeof(ContentCatalogProvider),typeof(ContentCatalogData)));
            File.WriteAllText(Path.Combine(output,"settings.json"),JsonUtility.ToJson(runtime,true));
            report.assetCount=mapping.mapping.Length;
            report.assetKeyCount=mapping.mapping.Sum(e=>e.keys.Length);
            report.output=output;
            report.passed=true;
        }
        catch(Exception error){report.exception=error.ToString();Debug.LogException(error);}
        File.WriteAllText(Path.Combine(recovery,"Evidence/addressables-windows-build.json"),JsonUtility.ToJson(report,true));
        Debug.Log("RECOVERED_ADDRESSABLES_BUILD passed="+report.passed+" assets="+report.assetCount);
        EditorApplication.Exit(report.passed?0:1);
    }
}
