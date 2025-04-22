using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for path generation helper

// --- Game Namespaces (Mono) ---
using ScheduleOne.Property;
using ScheduleOne.Map; // For ManorGate
// --- End Game Namespaces ---

// --- Editor/Serialization ---
// Required for OnValidate path generation
#if UNITY_EDITOR
using UnityEditor;
#endif
// Requires Newtonsoft.Json package added to your Unity project
using Newtonsoft.Json;
// --- End Editor/Serialization ---

// --- Unity Event System ---
using UnityEngine.Events; // Required for UnityAction
// --- End Unity Event System ---

namespace ManorMod
{
    // This component MUST be attached to the root of the ManorSetup prefab
    // and have its fields assigned in the Unity Inspector.
    public class ManorConfiguration : PropertyConfiguration // Inherit from base
    {
        [Header("Manor Specific References (Assign in Inspector)")]
        [SerializeField] private ManorGate manorGate = null;
        [Tooltip("Optional: Reference to the listing poster object WITHIN this prefab.")]
        [SerializeField] private Transform listingPosterTransform = null; // Changed to Transform

        [Header("Scene Objects To Disable (Editor Only - Drag from Scene)")]
        [Tooltip("Drag GameObjects from the SCENE here IN THE EDITOR to mark them for disabling at runtime.")]
        [SerializeField] private List<Transform> sceneObjectsToDisable = new List<Transform>();

        [Header("Runtime Data (Read Only)")]
        [Tooltip("Populated automatically in Editor from the list above.")]
        [SerializeField] [TextArea(7, 15)] private string sceneObjectPathsJson;

        // Public accessor for MainMod to get the paths JSON
        public string SceneObjectPathsJson => sceneObjectPathsJson;

        // --- Editor-Only Logic ---
#if UNITY_EDITOR
        // This method runs whenever values are changed in the Inspector (Editor only)
        private void OnValidate()
        {
            var paths = new List<string>();
            if (sceneObjectsToDisable != null)
            {
                foreach (Transform t in sceneObjectsToDisable)
                {
                    if (t != null)
                    {
                        // Ensure the object is actually in the scene, not part of this prefab instance
                        if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(t.gameObject) && t.gameObject.scene.IsValid())
                        {
                             string path = GetGameObjectPath(t.gameObject);
                             if (!string.IsNullOrEmpty(path))
                                 paths.Add(path);
                        }
                        else
                        {
                            // Optional: Log warning if user drags prefab objects here
                            // Debug.LogWarning($"Object '{t.name}' is part of a prefab, not the scene. Cannot get scene path.", this);
                        }
                    }
                }
            }
            // Use Newtonsoft.Json for serialization
            sceneObjectPathsJson = JsonConvert.SerializeObject(paths, Formatting.Indented);

            // Mark object dirty so changes to sceneObjectPathsJson are saved
            UnityEditor.EditorUtility.SetDirty(this);
        }

        // Helper to get the absolute path of a GameObject in the scene hierarchy (Editor only)
        private static string GetGameObjectPath(GameObject obj)
        {
            // From https://docs.unity3d.com/ScriptReference/AnimationUtility.CalculateTransformPath.html
             if (obj == null) return null;
             Transform current = obj.transform;
             string path = current.name;
             while (current.parent != null)
             {
                 current = current.parent;
                 // Stop if we reach the scene root (parent is null)
                 if (current.parent == null) break;
                 path = current.name + "/" + path;
             }
             // Scene objects should start with "/" according to GameObject.Find convention
             return "/" + path;
        }
#endif

        // --- Runtime Logic ---

        private Property _manorProperty; // Cache parent property

        void Start()
        {
            MelonLogger.Msg($"{this.GetType().Name}: Start() on {gameObject.name}");
            _manorProperty = GetComponentInParent<Property>();

            if (_manorProperty == null)
            {
                MelonLogger.Error($"{this.GetType().Name}: Could not find Property component in parent! Aborting.");
                enabled = false;
                return;
            }

            // Configure using base class logic first
            // Now apply Manor-specific configurations
            ConfigureManorGate();
            ConfigureListingPosterReference(); // Assign prefab's poster ref to Property

            MelonLogger.Msg($"Manor configuration complete for '{_manorProperty.PropertyName}'.");
        }

        private void ConfigureManorGate()
        {
            if (manorGate == null)
            {
                 // Base class ConfigureProperty already warned if npcSpawnPoint is null
                 MelonLogger.Warning($"{this.GetType().Name}: ManorGate reference not assigned in inspector!");
                 return;
            }
            // Set initial state
            manorGate.SetEnterable(_manorProperty.IsOwned);
            // Add listener for property acquisition
            UnityAction gateAction = () => { if (manorGate != null) manorGate.SetEnterable(true); };
            _manorProperty.onThisPropertyAcquired.RemoveListener(gateAction);
            _manorProperty.onThisPropertyAcquired.AddListener(gateAction);
        }

        private void ConfigureListingPosterReference()
        {
            // Assign the reference from the prefab to the Property component
            // MainMod will handle moving the actual object later using Property.ListingPoster.
            if (listingPosterTransform != null)
            {
                 _manorProperty.ListingPoster = listingPosterTransform;
            }
             else
             {
                  MelonLogger.Warning($"{this.GetType().Name}: Listing Poster Transform reference not assigned in inspector!");
             }
        }
    }
}
