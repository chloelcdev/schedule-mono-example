using UnityEngine;
using System.Collections.Generic;

// --- Game Namespaces (Mono) ---
using ScheduleOne.Property;
using ScheduleOne.Delivery; // For LoadingDock
using ScheduleOne.Interaction; // For InteractableToggleable
using ScheduleOne.Tiles;
using ScheduleOne.Misc;
using FluffyUnderware.DevTools.Extensions; // For ModularSwitch
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
        const string AgencyWhiteboardPath = "/Map/Container/RE Office/Interior/Whiteboard";


        Property sceneProperty = null;
        public Property SceneProperty 
        {
            get 
            {
                if (sceneProperty == null)
                    sceneProperty = PropertyManager.Instance?.GetProperty(propertyCode);

                return sceneProperty;
            }
        }


        [Tooltip("The property-code of the property to configure.")]
        public string propertyCode = "manor";


        [Tooltip("Price the property will be set to.")]
        public float propertyPrice = 100000f;


        [Tooltip("Employee capacity the property will be set to. YOU MUST HAVE ENOUGH IDLE POINTS FOR THIS MANY EMPLOYEES OR IT WILL CRASH WHEN YOU HIRE PEOPLE.")]
        public int propertyEmployeeCapacity = 10;


        [Tooltip("If true, the default employee idle points will be replaced with the ones provided, instead of combining them.")]
        public bool ReplaceEmployeeIdlePoints = true;


        [Tooltip("Employee idle points to add or replace the default ones with.")]
        public Transform[] employeeIdlePoints = new Transform[0];


        [Tooltip("Loading docks to add")]
        public LoadingDock[] ModLoadingDocks = new LoadingDock[0];


        [Tooltip("NPC spawn point transform to replace the default one with.")]
        public Transform npcSpawnPoint = null;


        [Tooltip("Modular switches to register.")]
        public ModularSwitch[] switches = new ModularSwitch[0];


        [Tooltip("Interactable toggleables to register.")]
        public InteractableToggleable[] toggleables = new InteractableToggleable[0];


        [Tooltip("Reference to the listing poster object to be added to the agency whiteboard.")]
        [SerializeField] public Transform ListingPosterTransform = null;

        bool hasRun = false;


        public void Awake() 
        {
            if (SceneProperty != null && !hasRun)
                ReconfigureProperty(SceneProperty);
        }

        /// <summary>
        /// Configures the provided Property component using the serialized fields.
        /// Called in Start()
        /// </summary>
        public virtual void ReconfigureProperty(Property property)
        {
            if (SceneProperty == null) return;

            hasRun = true;

            // Basic Stats
            property.Price = propertyPrice;
            property.EmployeeCapacity = propertyEmployeeCapacity;

            // NPC Spawn
            property.NPCSpawnPoint = npcSpawnPoint;

            // Loading Docks
            RegisterLoadingDocks(property);

            // Switches
            RegisterSwitches(property);

            // Toggleables
            RegisterToggleables(property);

            // Idle Points
            RegisterIdlePoints(property);

            // Listing Poster
            MoveListingPosterToWhiteboard();

            // Save Point
            RegisterSavePoint(property);
        }

        // --- Helper Configuration Methods ---

        private void RegisterLoadingDocks(Property property)
        {
            // Set each docks ParentProperty to be the property
            foreach (var dock in ModLoadingDocks.Where(d => d != null))
                dock.ParentProperty = property;

            // Make sure the property has a LoadingDocks array
            if (property.LoadingDocks == null)
                property.LoadingDocks = new LoadingDock[0];

            // add loading docks to the property
            property.LoadingDocks.AddRange(ModLoadingDocks);
        }

        private void RegisterSwitches(Property property)
        {

            if (property.Switches == null)
                property.Switches = new();

            property.Switches.AddRange(switches);
        }

        private void RegisterToggleables(Property property)
        {
            
            if (property.Toggleables == null)
                property.Toggleables = new();

            property.Toggleables.AddRange(toggleables);
        }

        private void RegisterIdlePoints(Property property)
        {
            if (property.EmployeeIdlePoints == null || ReplaceEmployeeIdlePoints)
                property.EmployeeIdlePoints = new Transform[0];

            property.EmployeeIdlePoints.AddRange(employeeIdlePoints);
        }

        private void MoveListingPosterToWhiteboard()
        {
            if (ListingPosterTransform == null) return;
            GameObject targetWhiteboard = GameObject.Find(AgencyWhiteboardPath);
            ListingPosterTransform.SetParent(targetWhiteboard.transform, true);
        }

        private void RegisterSavePoint(Property property)
        {
            // mmmmm we may not actually have to do anything with this, I think the games SavePoint class is self-sufficient
        }
    }
}