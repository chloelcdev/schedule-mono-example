using MelonLoader;
using HarmonyLib;
using UnityEngine;
// --- Networking Namespaces (Mono?) ---
using FishNet.Object;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet; // Base FishNet namespace
               // --- End Networking Namespaces ---

[assembly: MelonInfo(typeof(ManorMod.MainMod), ManorMod.BuildInfo.Name, ManorMod.BuildInfo.Version, ManorMod.BuildInfo.Author, ManorMod.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ManorMod
{
    public static class BuildInfo
    {
        public const string Name = "ManorMod";
        public const string Description = "Adds a functional Manor property.";
        public const string Author = "ChloeNow";
        public const string Company = null;
        public const string Version = "1.0.0-mono";
        public const string DownloadLink = null;
    }

    public class MainMod : MelonMod
    {
        // Constants
        private const string BundleName = "chloemanorsetup";
        private const string PrefabName = "ManorSetup-Chloe";
        private const string TargetSceneName = "Main";
        private const string ManorPropertyCode = "manor";
        private const string WhiteboardPath = "/Map/Container/RE Office/Interior/Whiteboard";

        // Static references
        private static AssetBundle customAssetsBundle;
        private static GameObject manorSetupPrefab;
        private static GameObject spawnedInstanceRoot = null;
        private static NetworkObject spawnedNetworkObject = null;
        internal static HarmonyLib.Harmony HarmonyInstance { get; private set; }

        // List of objects to disable
        private static readonly List<string> objectsToDisableBeforeSetup = new List<string>
        {
            "@Properties/Manor/House/Door Frames/Mansion Door Frame",
            "@Properties/Manor/House/MansionDoor",
            "@Properties/Manor/House/mansion/DoorFrame",
        };

        // --- MelonMod Overrides ---

        public override void OnInitializeMelon()
        {
            //HarmonyInstance = new HarmonyLib.Harmony(Info.a.GetName().Name);
            LoggerInstance.Msg($"{BuildInfo.Name} v{BuildInfo.Version} Initializing...");

            LoadAssetBundle();
            DialoguePatcher.InitializePatches(HarmonyInstance);

            LoggerInstance.Msg("Initialization complete.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == TargetSceneName)
            {
                LoggerInstance.Msg($"'{TargetSceneName}' scene loaded. Starting setup sequence.");
                MelonCoroutines.Start(SetupSequence());
            }
            else
            {
                CleanUp(); // Cleanup if leaving the main scene
            }
        }

        public override void OnApplicationQuit()
        {
            CleanUp();
            DialoguePatcher.RemovePatches(HarmonyInstance);
        }

        // --- Setup Logic ---

        private IEnumerator SetupSequence()
        {
            yield return null; // Wait a frame for scene to fully load

            DisableOriginalObjects();
            LoadPrefabsFromBundle(); // Ensure prefab is loaded

            Property manorProperty = FindManor();
            if (manorProperty == null)
            {
                LoggerInstance.Error("Cannot proceed with setup: Manor Property not found.");
                yield break; // Stop coroutine
            }

            SpawnPrefabInstance(); // Tries network then local spawn

            if (spawnedInstanceRoot == null)
            {
                LoggerInstance.Error("Cannot proceed with setup: Prefab instance failed to spawn.");
                yield break; // Stop coroutine
            }

            AddComponentsIfMissing(spawnedInstanceRoot);
            ParentPrefabToProperty(spawnedInstanceRoot, manorProperty);
            RelocateListingPoster(spawnedInstanceRoot);

            LoggerInstance.Msg("Setup sequence complete.");
        }

        private void LoadAssetBundle()
        {
            if (customAssetsBundle != null) return;
            LoggerInstance.Msg($"Loading AssetBundle '{BundleName}'...");
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"{typeof(MainMod).Namespace}.{BundleName}";
                using (System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) { LoggerInstance.Error($"Failed to find embedded resource stream: {resourceName}"); return; }

                    // Read stream to byte array
                    byte[] assetData;
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        assetData = memoryStream.ToArray();
                    }

                    // Use standard AssetBundle loading for Mono
                    customAssetsBundle = AssetBundle.LoadFromMemory(assetData);
                    if (customAssetsBundle == null) LoggerInstance.Error("AssetBundle.LoadFromMemory returned null!");
                    else LoggerInstance.Msg($"AssetBundle '{BundleName}' loaded.");
                }
            }
            catch (Exception e) { LoggerInstance.Error($"Exception loading AssetBundle: {e}"); customAssetsBundle = null; }
        }

        private void LoadPrefabsFromBundle()
        {
            if (customAssetsBundle == null) { LoggerInstance.Error("Cannot load prefab, AssetBundle is null."); return; }
            if (manorSetupPrefab != null) return;
            try
            {
                // Load using UnityEngine.AssetBundle
                manorSetupPrefab = customAssetsBundle.LoadAsset<GameObject>(PrefabName);
                if (manorSetupPrefab == null) LoggerInstance.Error($"Failed to load prefab '{PrefabName}' from bundle!");
                // else LoggerInstance.Msg($"Prefab '{PrefabName}' loaded."); // Less noise
            }
            catch (Exception e) { LoggerInstance.Error($"Error loading prefab: {e}"); manorSetupPrefab = null; }
        }

        private Property FindManor()
        {
            // Use ScheduleOne namespace
            if (ScheduleOne.Property.PropertyManager.Instance == null) { LoggerInstance.Error("FindManor: PropertyManager instance not found!"); return null; }
            Property prop = ScheduleOne.Property.PropertyManager.Instance.GetProperty(ManorPropertyCode);
            return prop;
        }

        private void DisableOriginalObjects()
        {
            int disabledCount = 0;
            foreach (string path in objectsToDisableBeforeSetup)
            {
                GameObject objToDisable = GameObject.Find(path);
                if (objToDisable != null)
                {
                    objToDisable.SetActive(false);
                    disabledCount++;
                }
            }
            LoggerInstance.Msg($"Disabled {disabledCount} original Manor objects.");
        }

        private void SpawnPrefabInstance()
        {
            if (manorSetupPrefab == null) { LoggerInstance.Error("Manor setup prefab not loaded. Cannot spawn."); return; }
            if (spawnedInstanceRoot != null) { LoggerInstance.Warning("Manor setup instance already exists. Skipping spawn."); return; }

            bool spawnedNetworked = false;
            // Use FishNet namespace directly
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            if (networkManager != null && networkManager.ServerManager.Started)
            {
                NetworkObject prefabNob = manorSetupPrefab.GetComponent<NetworkObject>();
                if (prefabNob != null)
                {
                    GameObject instanceToSpawn = null;
                    try
                    {
                        instanceToSpawn = GameObject.Instantiate(manorSetupPrefab, Vector3.zero, Quaternion.identity);
                        NetworkObject nobToSpawn = instanceToSpawn?.GetComponent<NetworkObject>();
                        if (nobToSpawn != null)
                        {
                            ServerManager serverManager = networkManager.ServerManager;
                            serverManager.Spawn(nobToSpawn, null); // Spawn for all clients
                            spawnedNetworkObject = nobToSpawn;
                            spawnedInstanceRoot = instanceToSpawn;
                            spawnedInstanceRoot.name = PrefabName + "_NetworkInstance";
                            spawnedNetworked = true;
                            LoggerInstance.Msg($"Network Spawn successful: {spawnedInstanceRoot.name} (ID: {nobToSpawn.ObjectId}) ");
                        }
                        else { LoggerInstance.Error("Instantiated prefab missing NetworkObject! Destroying instance."); if(instanceToSpawn != null) GameObject.Destroy(instanceToSpawn); }
                    }
                    catch (Exception e) { LoggerInstance.Error($"Exception during Network Spawn attempt: {e}"); if (instanceToSpawn != null) GameObject.Destroy(instanceToSpawn); spawnedNetworked = false; spawnedInstanceRoot = null; spawnedNetworkObject = null; }
                }
                else LoggerInstance.Warning($"Prefab '{PrefabName}' missing NetworkObject component. Falling back to local spawn.");
            }

            if (!spawnedNetworked)
            {
                try
                {
                    spawnedInstanceRoot = GameObject.Instantiate(manorSetupPrefab, Vector3.zero, Quaternion.identity);
                    if (spawnedInstanceRoot != null)
                    {
                        spawnedInstanceRoot.name = PrefabName + "_LocalInstance";
                        LoggerInstance.Msg($"Local Instantiate successful: {spawnedInstanceRoot.name}");
                    }
                    else { LoggerInstance.Error("Local Instantiate returned null!"); }
                }
                catch (Exception e) { LoggerInstance.Error($"Exception during Local Instantiate: {e}"); spawnedInstanceRoot = null; }
            }
        }

        private void AddComponentsIfMissing(GameObject instance)
        {
            if (instance == null) return;
            if (instance.GetComponent<ManorConfiguration>() == null)
            {
                LoggerInstance.Warning($"Adding missing ManorConfiguration component to {instance.name} at runtime.");
                instance.AddComponent<ManorConfiguration>();
            }
            // Removed teleporter check
        }

        private void ParentPrefabToProperty(GameObject instance, Property targetParentProperty)
        {
            if (instance == null || targetParentProperty == null) return;
            try
            {
                instance.transform.SetParent(targetParentProperty.transform, true); // Keep world position
                instance.SetActive(true); // Ensure components initialize after parenting
                LoggerInstance.Msg($"Parented '{instance.name}' to '{targetParentProperty.PropertyName}'.");
                // TODO: Add calls to ComponentRestorer or ShaderFix if necessary
            }
            catch (Exception e)
            {
                LoggerInstance.Error($"Exception during SetParent: {e}");
                if (instance != null) GameObject.Destroy(instance);
                spawnedInstanceRoot = null;
                spawnedNetworkObject = null;
            }
        }

        private void RelocateListingPoster(GameObject prefabInstance)
        {
            if (prefabInstance == null) return;
            string listingPosterName = "PropertyListing Hilltop Manor"; // TODO: Make const

            // Use helper extension method
            Transform sourceListing = prefabInstance.transform.FindDeepChild(listingPosterName);
            if (sourceListing == null) { LoggerInstance.Warning($"RelocateListingPoster: Could not find '{listingPosterName}' in instance '{prefabInstance.name}'."); return; }

            GameObject targetWhiteboard = GameObject.Find(WhiteboardPath);
            if (targetWhiteboard == null) { LoggerInstance.Error($"RelocateListingPoster: Could not find target whiteboard '{WhiteboardPath}'."); return; }

            try
            {
                sourceListing.SetParent(targetWhiteboard.transform, true);
                sourceListing.gameObject.SetActive(true);
                LoggerInstance.Msg($"Relocated '{sourceListing.name}' to '{targetWhiteboard.name}'.");
            }
            catch (Exception ex) { LoggerInstance.Error($"Failed to reparent listing object: {ex.Message}"); }
        }

        // --- Cleanup ---

        private void CleanUp()
        {
            if (spawnedNetworkObject != null)
            {
                NetworkManager networkManager = InstanceFinder.NetworkManager;
                if (networkManager != null && networkManager.ServerManager.Started)
                {
                    try { networkManager.ServerManager.Despawn(spawnedNetworkObject.gameObject, DespawnType.Destroy); }
                    catch (Exception e) { LoggerInstance.Error($"Exception during Network Despawn: {e}"); if (spawnedInstanceRoot != null) GameObject.Destroy(spawnedInstanceRoot); }
                }
                else if (spawnedInstanceRoot != null) GameObject.Destroy(spawnedInstanceRoot);
            }
            else if (spawnedInstanceRoot != null)
            {
                GameObject.Destroy(spawnedInstanceRoot);
            }
            spawnedInstanceRoot = null;
            spawnedNetworkObject = null;

            // Consider unloading the asset bundle here if appropriate
            if (customAssetsBundle != null)
            {
                 customAssetsBundle.Unload(false); // false to unload compressed data but keep loaded assets
                 customAssetsBundle = null;
                 LoggerInstance.Msg("Unloaded asset bundle.");
            }
        }

        // --- Debug --- (Optional)
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F7))
                OnDebugKey();
        }

        void OnDebugKey()
        {
            LoggerInstance.Msg("F7 ManorMod debug key pressed.");
            // TODO: Add useful debug actions
        }
    }
}
