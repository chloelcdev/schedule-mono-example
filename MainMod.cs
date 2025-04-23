using MelonLoader;
using UnityEngine;
using System.Collections; // Added for IEnumerator
using ScheduleOne.Property; // Added for Property type
using FishNet;


[assembly: MelonInfo(typeof(ManorMod.MainMod), ManorMod.BuildInfo.Name, ManorMod.BuildInfo.Version, ManorMod.BuildInfo.Author, ManorMod.BuildInfo.DownloadLink)]
[assembly: MelonColor(245,180,20,255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ManorMod
{
    public static class BuildInfo
    {
        public const string Name = "ManorMod";
        public const string Description = "Adds a functional Manor property.";
        public const string Author = "ChloeNow";
        public const string Company = null;
        public const string Version = "0.9.1";
        public const string DownloadLink = null;
    }

    public class MainMod : MelonMod
    {
        // Constants
        private const string PrefabName = "ManorSetup-Chloe";
        private const string TargetSceneName = "Main";
        private const string ManorPropertyCode = "manor";
        private const string AssetBundleName = "chloemanorsetup"; // Lowercase convention

        // Static references
        private static GameObject prefabToSpawn;
        private static GameObject spawnedInstanceRoot = null;
        
        private static AssetBundle _loadedBundle = null;

        
        // static manor reference for easy access
        static Property sceneProperty = null;
        public static Property SceneProperty 
        {
            get 
            {
                if (sceneProperty == null)
                    sceneProperty = PropertyManager.Instance?.GetProperty(ManorPropertyCode);

                return sceneProperty;
            }
        }


        // --- MelonMod Overrides ---

        public override void OnInitializeMelon()
        {
            MelonCoroutines.Start(LoadAssetBundle());
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == TargetSceneName)
                MelonCoroutines.Start(SetupSequence());
        }


        // --- Setup Logic ---

        private IEnumerator SetupSequence()
        {
            //if (!InstanceFinder.IsHost)
            //    yield break;


            if (SceneProperty == null)
                yield break; // Stop coroutine

            yield return null; // Wait a frame after loading

            LoadPrefabFromBundle();

            if (prefabToSpawn == null) 
            {
                MelonLogger.Msg("Failed to load ManorSetup prefab from bundle.");
                yield break; // Stop coroutine
            }


            SpawnPrefabInstance();

            if (spawnedInstanceRoot == null)
            {
                MelonLogger.Msg("Failed to spawn ManorSetup prefab.");
                yield break; // Stop coroutine
            }

            // grab our manor config component
            PropertyConfiguration manorConfig = spawnedInstanceRoot.GetComponentInChildren<PropertyConfiguration>(true);
            manorConfig.ReconfigureProperty(SceneProperty);
            spawnedInstanceRoot.transform.SetParent(SceneProperty.transform, true);
        }

        private IEnumerator LoadAssetBundle()
        {
            if (_loadedBundle != null)
            {
                yield break; // Exit if already loaded
            }

            string executingAssemblyPath = Path.GetDirectoryName(MelonAssembly.Location);
            if (executingAssemblyPath == null)
            {
                 yield break;
            }

            string bundlePath = Path.Combine(executingAssemblyPath, AssetBundleName);

            if (!File.Exists(bundlePath))
            {
                yield break;
            }

            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return bundleLoadRequest;

            if (bundleLoadRequest.assetBundle == null)
            {
                yield break;
            }

            _loadedBundle = bundleLoadRequest.assetBundle;
        }

        private void LoadPrefabFromBundle()
        {
            if (_loadedBundle == null) return; // Check the correct bundle variable
            if (prefabToSpawn != null) return;

            // Load using UnityEngine.AssetBundle from the correct bundle
            prefabToSpawn = _loadedBundle.LoadAsset<GameObject>(PrefabName);
            if (prefabToSpawn == null)
                MelonLogger.Msg("Failed to load ManorSetup prefab from bundle."); // Keep essential log
        }

        private void SpawnPrefabInstance()
        {
            if (prefabToSpawn == null) return;
            spawnedInstanceRoot = GameObject.Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
            InstanceFinder.NetworkManager.ServerManager.Spawn(spawnedInstanceRoot, InstanceFinder.ClientManager.Connection);
        }
    }
}