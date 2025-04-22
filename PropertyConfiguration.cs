using UnityEngine;
using System.Collections.Generic;

// --- Game Namespaces (Mono) ---
using ScheduleOne.Property;
using ScheduleOne.Delivery; // For LoadingDock
using ScheduleOne.Interaction; // For InteractableToggleable
using ScheduleOne.Tiles; // For ModularSwitch
// --- End Game Namespaces ---

namespace ManorMod
{
    /// <summary>
    /// Base class for configuring a Property component using serialized references.
    /// Meant to be attached to a prefab that will be parented to a Property.
    /// Assign references in the Unity Inspector.
    /// </summary>
    public abstract class PropertyConfiguration : MonoBehaviour
    {
        [Header("Base Property Settings")]
        [Tooltip("Price the property will be set to.")]
        [SerializeField] protected float propertyPrice = 100000f;
        [Tooltip("Employee capacity the property will be set to.")]
        [SerializeField] protected int propertyEmployeeCapacity = 10;

        [Header("Common Prefab References (Assign in Inspector)")]
        [Tooltip("Loading docks that belong to this property.")]
        [SerializeField] protected LoadingDock[] loadingDocks = System.Array.Empty<LoadingDock>();
        [Tooltip("NPC spawn point transform for this property.")]
        [SerializeField] protected Transform npcSpawnPoint = null;
        [Tooltip("Modular switches within this property prefab.")]
        [SerializeField] protected ModularSwitch[] switches = System.Array.Empty<ModularSwitch>();
        [Tooltip("Interactable toggleables within this property prefab.")]
        [SerializeField] protected InteractableToggleable[] toggleables = System.Array.Empty<InteractableToggleable>();
        [Tooltip("Array of transforms representing additional employee idle points.")]
        [SerializeField] protected Transform[] employeeIdlePoints = System.Array.Empty<Transform>();

        /// <summary>
        /// Configures the provided Property component using the serialized fields.
        /// Called by derived classes in Start() after finding the parent Property.
        /// </summary>
        protected virtual void ConfigureProperty(Property property)
        {
            if (property == null) return;

            // Basic Stats
            property.Price = propertyPrice;
            property.EmployeeCapacity = propertyEmployeeCapacity;

            // NPC Spawn
            property.NPCSpawnPoint = npcSpawnPoint;

            // Loading Docks
            ConfigureLoadingDocks(property);

            // Switches
            ConfigureSwitches(property);

            // Toggleables
            ConfigureToggleables(property);

            // Idle Points
            MergeIdlePoints(property);
        }

        // --- Helper Configuration Methods ---

        private void ConfigureLoadingDocks(Property property)
        {
            // Assuming Property.LoadingDocks is List<LoadingDock>
            if (loadingDocks == null || loadingDocks.Length == 0)
            {
                if (property.LoadingDocks != null && property.LoadingDocks.Count > 0) property.LoadingDocks.Clear();
                else if (property.LoadingDocks == null) property.LoadingDocks = new List<LoadingDock>();
                return;
            }
            foreach (var dock in loadingDocks) if (dock != null) dock.ParentProperty = property;
            property.LoadingDocks = new List<LoadingDock>(loadingDocks);
            // If T[]: property.LoadingDocks = loadingDocks;
        }

        private void ConfigureSwitches(Property property)
        {
            // Assuming Property.Switches is List<ModularSwitch>
            if (switches == null || switches.Length == 0)
            {
                if (property.Switches != null && property.Switches.Count > 0) property.Switches.Clear();
                else if (property.Switches == null) property.Switches = new List<ModularSwitch>();
                return;
            }
            property.Switches = new List<ModularSwitch>(switches);
            // If T[]: property.Switches = switches;
            // Listeners likely handled by Property.Awake
        }

        private void ConfigureToggleables(Property property)
        {
            // Assuming Property.Toggleables is List<InteractableToggleable>
             if (toggleables == null || toggleables.Length == 0)
             {
                 if (property.Toggleables != null && property.Toggleables.Count > 0) property.Toggleables.Clear();
                 else if (property.Toggleables == null) property.Toggleables = new List<InteractableToggleable>();
                 return;
             }
            property.Toggleables = new List<InteractableToggleable>(toggleables);
            // If T[]: property.Toggleables = toggleables;

            // Re-attach listeners - safer to include
            foreach (var toggleable in property.Toggleables)
            {
                if (toggleable != null)
                {
                    System.Action toggleAction = () => PropertyToggleableActioned(property, toggleable);
                    toggleable.onToggle.RemoveListener(toggleAction);
                    toggleable.onToggle.AddListener(toggleAction);
                }
            }
        }

        private void PropertyToggleableActioned(Property property, InteractableToggleable toggleable)
        {
             if (property != null && toggleable != null) property.HasChanged = true;
        }

        private void MergeIdlePoints(Property property)
        {
            // Assuming List<Transform>
            var currentPoints = property.EmployeeIdlePoints ?? new List<Transform>();
            int addedCount = 0;
            if (employeeIdlePoints != null)
            {
                foreach (Transform point in employeeIdlePoints)
                {
                    if (point != null && !currentPoints.Contains(point))
                    {
                        currentPoints.Add(point);
                        addedCount++;
                    }
                }
            }
            if (addedCount > 0 || property.EmployeeIdlePoints == null)
            {
                 property.EmployeeIdlePoints = currentPoints;
                 // If T[]: property.EmployeeIdlePoints = currentPoints.ToArray();
            }
        }
    }
} 