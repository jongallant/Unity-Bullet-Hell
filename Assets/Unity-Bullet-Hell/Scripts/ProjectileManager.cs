using UnityEngine;
using System.Collections.Generic;

namespace BulletHell
{
    public class ProjectileManager : MonoBehaviour
    {
        private bool Initialized = false;

        // Lists out all the projectile types found in Assets
        [SerializeField]
        private List<ProjectileType> ProjectileTypes;

        // Each projectile type has its own material, therefore, own IndirectRenderer
        private Dictionary<int, IndirectRenderer> IndirectRenderers;

        // Cache the last accessed IndirectRenderer to reduce the Dict lookup for batches
        private int LastAccessedProjectileTypeIndex = -1;
        private IndirectRenderer LastAccessedRenderer;

        // Counters to keep track of Projectile Group information
        private Dictionary<int, ProjectileTypeCounters> ProjectileTypeCounters;

        [SerializeField]
        private ProjectileEmitterBase[] EmittersArray;
        private int MaxEmitters = 100;
        private int EmitterCount = 0;

        // Singleton
        private static ProjectileManager instance = null;
        public static ProjectileManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ProjectileManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject();
                        go.name = "ProjectileManager";
                        instance = go.AddComponent<ProjectileManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        void Awake()
        {
            Instance.Initialize();
        }

        [RuntimeInitializeOnLoadMethod]
        static void EnableInstance()
        {
            Instance.Initialized = false;
        }

        void Initialize()
        {
            if (!Initialized)
            {
                // Grab a list of Projectile Types founds in assets folder "ProjectileTypes"
                GameObject[] projectileTypes = Resources.LoadAll<GameObject>("ProjectileTypes");
                ProjectileTypes = new List<ProjectileType>();
                IndirectRenderers = new Dictionary<int, IndirectRenderer>();
                ProjectileTypeCounters = new Dictionary<int, ProjectileTypeCounters>();

                // Process projectile types
                for (int n = 0; n < projectileTypes.Length; n++)
                {
                    ProjectileType type = projectileTypes[n].GetComponent<ProjectileType>();
                    type.SetIndex(n);
                    ProjectileTypes.Add(type);

                    // If material is set to be a static color ensure we do not send color data to shader
                    float isStaticFloat = type.Material.GetFloat("_StaticColor");
                    bool isStatic = false;
                    if (isStaticFloat == 1)
                        isStatic = true;

                    IndirectRenderers.Add(n, new IndirectRenderer(type.MaxProjectileCount, type.Material, type.Mesh, isStatic));
                    ProjectileTypeCounters.Add(n, new ProjectileTypeCounters());
                }


                EmittersArray = new ProjectileEmitterBase[MaxEmitters];

                // Get a list of all emitters in the scene
                RegisterEmitters();

                Initialized = true;
            }
        }

        // Adds a new emitter at runtime
        public void AddEmitter(ProjectileEmitterBase emitter, int allocation)
        {
            // Default projectile if no projectile type set
            if (emitter.ProjectileType == null)
                emitter.ProjectileType = GetProjectileType(0);

            // Increment group counter
            ProjectileTypeCounters[emitter.ProjectileType.Index].TotalGroups++;

            // Should be a way to not allocate more than projectile type will allow - across all emitters
            emitter.Initialize(allocation);

            EmitterCount++;
        }

        // Only should be used in DEBUG mode when adding Emitters during runtime
        // This rebuilds the entire Emitters array -- If adding emitters at runtime you should use the AddEmitter method.
        private void RefreshEmitters()
        {
            ProjectileEmitterBase[] emittersTemp = GameObject.FindObjectsOfType<ProjectileEmitterBase>();

            if (emittersTemp.Length != EmitterCount)
            {
                EmitterCount = 0;

                // reset group counter
                for (int n = 0; n < ProjectileTypes.Count; n++)
                {
                    ProjectileTypeCounters[ProjectileTypes[n].Index].TotalGroups = 0;
                    ProjectileTypeCounters[ProjectileTypes[n].Index].TotalProjectilesAssigned = 0;
                }

                for (int n = 0; n < EmittersArray.Length; n++)
                {
                    if (EmittersArray[n] != null)
                    {
                        EmittersArray[n].ClearAllProjectiles();
                        EmittersArray[n] = null;
                    }
                }

                RegisterEmitters();
            }
        }

        public void RegisterEmitters()
        {
            // Register Emitters that are currently in the scene
            ProjectileEmitterBase[] emittersTemp = GameObject.FindObjectsOfType<ProjectileEmitterBase>();

            for (int n = 0; n < emittersTemp.Length; n++)
            {
                EmittersArray[n] = emittersTemp[n];
                // Default projectile if no projectile type set
                if (EmittersArray[n].ProjectileType == null)
                    EmittersArray[n].ProjectileType = GetProjectileType(0);

                // Increment group counter
                ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].TotalGroups++;
            }

            // Initialize the emitters -- if value is set fo projectilePoolSize -- system will use it
            // Make sure to not assign projectilePoolSize larger than total material type projectile count
            for (int n = 0; n < EmittersArray.Length; n++)
            {
                if (EmittersArray[n] != null)
                {
                    int projectilesToAssign = EmittersArray[n].ProjectilePoolSize;

                    if (projectilesToAssign == -1)
                    {
                        EmittersArray[n].ProjectilePoolSize = 1000;
                        projectilesToAssign = 1000;
                    }

                    // Old code to auto assign pool sizes based on total Projectile Group -- would split allocation across all emitters.
                    // This turns out to be problematic when adding/removing emitters on the fly.
                    // New system will allocate per the emitter.

                    // Total projectiles value not set on Emitter, Calculate max based on total groups even distribution
                    //if (projectilesToAssign < 0)
                    //{
                    //    projectilesToAssign = EmittersArray[n].ProjectileType.MaxProjectileCount / ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].TotalGroups;
                    //}
                    // Initialize Emitter pool size
                    EmittersArray[n].Initialize(projectilesToAssign);
                    ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].TotalProjectilesAssigned += projectilesToAssign;

                    EmitterCount++;
                }
            }

            // Check assignments - output error if too many assigned
            for (int n = 0; n < ProjectileTypes.Count; n++)
            {
                if (ProjectileTypeCounters[ProjectileTypes[n].Index].TotalProjectilesAssigned > ProjectileTypes[n].MaxProjectileCount)
                {
                    Debug.Log("Projectile Type '" + ProjectileTypes[n].name + "' emitters assigned too many projectiles:  " + ProjectileTypeCounters[ProjectileTypes[n].Index].TotalProjectilesAssigned + " assigned with max of " + ProjectileTypes[n].MaxProjectileCount + ".  Reduce Max Projectiles on Emitters that use this projectile type.");
                }
            }

        }

        // When adding emitter during play mode - you can register them with this function
        public void RegisterEmitter(ProjectileEmitterBase emitter)
        {
            // Should probably use Emittercount here
            int nextEmpty = -1;
            for (int n = 0; n < EmittersArray.Length; n++)
            {
                if (EmittersArray[n] == null)
                    nextEmpty = n;
            }

            if (nextEmpty == -1)
            {
                Debug.Log("Max Emitters reached.  Raise MaxEmitters if you need more.  Max set to " + MaxEmitters + ".");
            }
            else
            {
                EmittersArray[nextEmpty] = emitter;
                ProjectileTypeCounters[emitter.ProjectileType.Index].TotalGroups++;

                int projectilesToAssign = emitter.ProjectilePoolSize;

                if (projectilesToAssign == -1)
                {
                    emitter.ProjectilePoolSize = 1000;
                    projectilesToAssign = 1000;
                }

                // Old code to auto assign pool sizes based on total Projectile Group -- would split allocation across all emitters.
                // This turns out to be problematic when adding/removing emitters on the fly.
                // New system will allocate per the emitter.

                // Total projectiles value not set on Emitter, Calculate max based on total groups even distribution
                //if (projectilesToAssign < 0)
                //{
                //    projectilesToAssign = emitter.ProjectileType.MaxProjectileCount / ProjectileTypeCounters[emitter.ProjectileType.Index].TotalGroups;
                //}
                // Initialize Emitter pool size
                emitter.Initialize(projectilesToAssign);
                ProjectileTypeCounters[emitter.ProjectileType.Index].TotalProjectilesAssigned += projectilesToAssign;

                EmitterCount++;
            }

        }

        public ProjectileType GetProjectileType(int index)
        {
            return ProjectileTypes[index];
        }

        public void UpdateBufferData(int index, ProjectileType projectileType, ProjectileData data)
        {
            if (projectileType.Index != LastAccessedProjectileTypeIndex)
            {
                LastAccessedProjectileTypeIndex = projectileType.Index;
                LastAccessedRenderer = IndirectRenderers[LastAccessedProjectileTypeIndex];
            }

            LastAccessedRenderer.UpdateBufferData(projectileType.BufferIndex, data);
            projectileType.BufferIndex++;
        }

        public void Update()
        {
            // Example of adding an emitter at runtime
            //if (Input.GetKeyDown(KeyCode.F))
            //{
            //    GameObject go = GameObject.Instantiate(Resources.Load("Emitters/Spinner") as GameObject);
            //    go.transform.position = new Vector2(1, 0);
            //    AddEmitter(go.GetComponent<ProjectileEmitterAdvanced>(), 1000);
            //    RegisterEmitter(go.GetComponent<ProjectileEmitterAdvanced>());
            //}

            // Provides a simple way to update active Emitters if removing/adding them at runtime for debugging purposes
            // You should be using AddEmitter() if you want to add Emitters at runtime
#if UNITY_EDITOR
            RefreshEmitters();
#endif

            UpdateEmitters();
            DrawEmitters();
            ResolveLeakedTime();
        }

        public void UpdateEmitters()
        {
            //reset
            for (int n = 0; n < ProjectileTypes.Count; n++)
            {
                ProjectileTypeCounters[n].ActiveProjectiles = 0;
                ProjectileTypes[n].BufferIndex = 0;
            }

            for (int n = 0; n < EmittersArray.Length; n++)
            {
                if (EmittersArray[n] != null)
                {
                    if (EmittersArray[n].gameObject.activeSelf && EmittersArray[n].enabled)
                    {
                        EmittersArray[n].UpdateEmitter();
                        ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].ActiveProjectiles += EmittersArray[n].ActiveProjectileCount;
                    }
                    else
                    {
                        // if the gameobject was disabled then clear all projectiles from this emitter
                        EmittersArray[n].ClearAllProjectiles();
                    }
                }
            }
        }

        public void DrawEmitters()
        {
            // We draw all emitters at the same time based on their Projectile Type.  1 draw call per projectile type.
            for (int n = 0; n < ProjectileTypes.Count; n++)
            {
                if (ProjectileTypeCounters[ProjectileTypes[n].Index].ActiveProjectiles > 0)
                    IndirectRenderers[ProjectileTypes[n].Index].Draw(ProjectileTypeCounters[ProjectileTypes[n].Index].ActiveProjectiles);
            }
        }

        public void ResolveLeakedTime()
        {
            // When interval is elapsed we need to account for leaked time or our projectiles will not be synchronized properly
            for (int n = 0; n < EmittersArray.Length; n++)
            {
                if (EmittersArray[n] != null)
                {
                    if (EmittersArray[n].gameObject.activeSelf && EmittersArray[n].enabled)
                    {
                        EmittersArray[n].ResolveLeakedTime();
                    }
                }
            }
        }

        void OnGUI()
        {
            GUI.Label(new Rect(5, 5, 300, 20), "Projectiles: " + ProjectileTypeCounters[0].ActiveProjectiles.ToString());
        }

        void OnApplicationQuit()
        {
            foreach (KeyValuePair<int, IndirectRenderer> kvp in IndirectRenderers)
            {
                kvp.Value.ReleaseBuffers(true);
            }
        }

        void OnDisable()
        {
            foreach (KeyValuePair<int, IndirectRenderer> kvp in IndirectRenderers)
            {
                kvp.Value.ReleaseBuffers(true);
            }
        }
    }

    public class ProjectileTypeCounters
    {
        public int ActiveProjectiles;
        public int TotalProjectilesAssigned;
        public int TotalGroups;
    }
}