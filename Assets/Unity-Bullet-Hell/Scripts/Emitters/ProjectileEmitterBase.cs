using UnityEngine;


namespace BulletHell
{
    public enum CollisionDetectionType
    {
        Raycast,
        CircleCast
    };

    public abstract class ProjectileEmitterBase : MonoBehaviour
    {
        protected Mesh Mesh;
        protected Material Material;
        protected float Interval;

        // Each emitter has its own ProjectileData pool
        protected Pool<ProjectileData> Projectiles;
        protected Pool<ProjectileData> ProjectileOutlines;

        [SerializeField] public ProjectilePrefab ProjectilePrefab;

        [Foldout("General", true)]
        [Range(0.01f, 5f), SerializeField] protected float INTERVAL = 0.1f;
        [SerializeField] protected Vector2 Direction = Vector2.up;
        [SerializeField] protected float TimeToLive = 5;
        [Range(0.001f, 10f), SerializeField] protected float Speed = 1;
        [Range(1f, 1000f), SerializeField] protected float MaxSpeed = 100;
        [Range(0.01f, 2f), SerializeField] protected float Scale = 0.05f;
        [SerializeField] protected Gradient Color;
        [SerializeField] protected float RotationSpeed = 0;
        [SerializeField] protected bool AutoFire = true;
        [SerializeField] protected bool BounceOffSurfaces = true;        
        [SerializeField] protected bool CullProjectilesOutsideCameraBounds = true;
        [SerializeField] protected CollisionDetectionType CollisionDetection = CollisionDetectionType.CircleCast;
        [SerializeField] protected bool IsVariableTimeStep = false;
        [ConditionalField(nameof(IsVariableTimeStep), true), Range(0.01f, 0.02f), SerializeField] protected float FIXED_TIMESTEP_RATE = 0.01f;
        [Range(1, 1000000), SerializeField] public int ProjectilePoolSize = 1000;

        [Foldout("Outline", true)]
        [SerializeField] public bool DrawOutlines;
        [ConditionalField(nameof(DrawOutlines)), Range(0.0f, 1f), SerializeField] public float OutlineSize;
        [ConditionalField(nameof(DrawOutlines)), SerializeField] protected Gradient OutlineColor;

        [Foldout("Modifiers", true)]
        [SerializeField] protected Vector2 Gravity = Vector2.zero;
        [Range(0.0f, 1f), SerializeField] protected float BounceAbsorbtionY;
        [Range(0.0f, 1f), SerializeField] protected float BounceAbsorbtionX;                  
        [Range(-10f, 10f), SerializeField] protected float Acceleration = 0;

        // Current active projectiles from this emitter
        public int ActiveProjectileCount { get; protected set; }
        public int ActiveOutlineCount { get; protected set; }

        // Collision layer
        protected int LayerMask = 1;
        protected RaycastHit2D[] RaycastHitBuffer = new RaycastHit2D[1];

        // For cull check
        protected Plane[] Planes = new Plane[6];

        protected Camera Camera;
        protected ContactFilter2D ContactFilter;
        protected ProjectileManager ProjectileManager;

        float Timer = 0;        
        int ProjectilesWaiting;
        protected int[] ActiveProjectileIndexes;
        protected int ActiveProjectileIndexesPosition;

        public void Awake()
        {
            Interval = INTERVAL;
            Camera = Camera.main;
            
            ContactFilter = new ContactFilter2D
            {
                layerMask = LayerMask,
                useTriggers = false,
            };

            ProjectileManager = ProjectileManager.Instance;

            // If projectile type is not set, use default
            if (ProjectilePrefab == null)
                ProjectilePrefab = ProjectileManager.Instance.GetProjectilePrefab(0);
        }

        public void Initialize(int size)
        {
            Projectiles = new Pool<ProjectileData>(size);
            if (ProjectilePrefab.Outline != null)
            {
                ProjectileOutlines = new Pool<ProjectileData>(size);
            }

            ActiveProjectileIndexes = new int[size];
        }

        public void UpdateEmitter(float tick)
        {
            if (AutoFire)
            {
                Interval -= tick;
            }

            // Interval has expired
            while (Interval <= 0)
            {
                Interval += INTERVAL;

                if (IsVariableTimeStep)
                {
                    // Variable time step, we fire projectile immediately
                    float leakedTime = Mathf.Abs(Interval);
                    FireProjectile(Direction, leakedTime);
                }
                else
                {                    
                    // Fixed timestep, we must wait until next fixed frame to fire projectile
                    ProjectilesWaiting++;
                }
            }

           
            if (!IsVariableTimeStep)
            {
                bool updateExecuted = false;
                Timer += tick;

                // fixed timestep timer internal loop
                while (Timer > FIXED_TIMESTEP_RATE)
                {
                    while (ProjectilesWaiting > 0)
                    {
                        ProjectilesWaiting--;
                        FireProjectile(Direction, ProjectilesWaiting * FIXED_TIMESTEP_RATE);
                    }

                    Timer -= FIXED_TIMESTEP_RATE;

                    UpdateProjectiles(FIXED_TIMESTEP_RATE);
                    updateExecuted = true;
                }                      
                if (!updateExecuted)
                    UpdateBuffers(tick);    // update was not executed so re-use buffers but still remove tick from TTL
                else
                    UpdateBuffers(0);       // already removed tick from TTL - don't do it again
            }
            else
            {
                // Variable time step -- just update the projectiles with deltatime
                UpdateProjectiles(tick);
                UpdateBuffers(0);
            }            
        }

        // Function to rotate a vector by x degrees
        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;

            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);

            return v;
        }

        public abstract Pool<ProjectileData>.Node FireProjectile(Vector2 direction, float leakedTime);

        protected abstract void UpdateProjectile(ref Pool<ProjectileData>.Node node, float tick);

        protected abstract void UpdateProjectiles(float tick);

        protected void UpdateBuffers(float tick)
        {
            ActiveProjectileCount = 0;
            ActiveOutlineCount = 0;

            for (int i = 0; i < ActiveProjectileIndexes.Length; i++)
            {
                // End of array is set to -1
                if (ActiveProjectileIndexes[i] == -1)
                    break;

                Pool<ProjectileData>.Node node = Projectiles.GetActive(ActiveProjectileIndexes[i]);
               
                node.Item.TimeToLive -= tick;

                ProjectileManager.UpdateBufferData(ProjectilePrefab, node.Item);
                ActiveProjectileCount++;
            }

            // faster to do two loops than to update the outlines at the same time due to the renderer swapping
            for (int i = 0; i < ActiveProjectileIndexes.Length; i++)
            {
                // End of array is set to -1
                if (ActiveProjectileIndexes[i] == -1)
                    break;

                Pool<ProjectileData>.Node node = Projectiles.GetActive(ActiveProjectileIndexes[i]);

                    //handle outline
                if (node.Item.Outline.Item != null)
                {
                    ProjectileManager.UpdateBufferData(ProjectilePrefab.Outline, node.Item.Outline.Item);
                    ActiveOutlineCount++;
                }
            }
        }

        protected void ReturnNode(Pool<ProjectileData>.Node node)
        {
            if (node.Active)
            {
                node.Item.TimeToLive = -1;
                if (node.Item.Outline.Item != null)
                {
                    ProjectileOutlines.Return(node.Item.Outline.NodeIndex);
                    node.Item.Outline.Item = null;
                }

                Projectiles.Return(node.NodeIndex);
            }
        }

        public void ClearAllProjectiles()
        {
            for (int i = 0; i < Projectiles.Nodes.Length; i++)
            {
                if (Projectiles.Nodes[i].Active)
                {
                    Projectiles.Nodes[i].Item.TimeToLive = -1;
                    if (Projectiles.Nodes[i].Item.Outline.Item != null)
                    {
                        ProjectileOutlines.Return(Projectiles.Nodes[i].Item.Outline.NodeIndex);
                        Projectiles.Nodes[i].Item.Outline.Item = null;
                    }
                    
                    Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                }
            }
        }
        
    }
}