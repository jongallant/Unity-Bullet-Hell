using UnityEngine;
using System.Collections.Generic;

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

            // Get a list of all emitters in the scene
            RefreshEmitters();

            Initialized = true;
        }
    }

    public void RefreshEmitters()
    {
        int oldLength = 0;
        if (EmittersArray != null)
            oldLength = EmittersArray.Length;

        EmittersArray = GameObject.FindObjectsOfType<ProjectileEmitterBase>();

        if (oldLength != EmittersArray.Length)
        {
            // reset group counter
            for (int n = 0; n < ProjectileTypes.Count; n++)
            {
                ProjectileTypeCounters[ProjectileTypes[n].Index].TotalGroups = 0;
            }

            for (int n = 0; n < EmittersArray.Length; n++)
            {
                // Default projectile if no projectile type set
                if (EmittersArray[n].ProjectileType == null)
                    EmittersArray[n].ProjectileType = GetProjectileType(0);

                // Increment group counter
                ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].TotalGroups++;
            }

            // Initialize the emitters -- assigning each an equal distribution of allowed projectiles -- based on emitter count and MaxProjectiles set for this Projectile Type
            for (int n = 0; n < EmittersArray.Length; n++)
            {
                EmittersArray[n].Initialize(EmittersArray[n].ProjectileType.MaxProjectileCount / ProjectileTypeCounters[EmittersArray[n].ProjectileType.Index].TotalGroups);
            }
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

        LastAccessedRenderer.UpdateBufferData(ProjectileTypes[LastAccessedProjectileTypeIndex].BufferIndex, data);
        ProjectileTypes[LastAccessedProjectileTypeIndex].BufferIndex++;
    }
    
    public void Update()
    {
        RefreshEmitters();
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
            if (EmittersArray[n].gameObject.activeSelf && EmittersArray[n].enabled)
            {
                EmittersArray[n].ResolveLeakedTime();
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
    public int TotalGroups;
}