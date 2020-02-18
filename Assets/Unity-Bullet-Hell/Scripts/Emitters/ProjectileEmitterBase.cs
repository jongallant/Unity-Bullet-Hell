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
        protected float Interval;

        // Each emitter has its own ProjectileData pool
        protected Pool<ProjectileData> Projectiles;
        protected Pool<ProjectileData> ProjectileOutlines;

        [Foldout("Appearance", true)]
        public ProjectilePrefab ProjectilePrefab;
        [Range(0.01f, 2f)] public float Scale = 0.05f;
        public Gradient Color;

        [Foldout("General", true)]
        public float TimeToLive = 5;
        [Range(0.01f, 5f)] public float CoolOffTime = 0.1f;
        public bool AutoFire = true;
        public Vector2 Direction = Vector2.up;        
        [Range(0.001f, 10f)] public float Speed = 1;
        [Range(1f, 100f)] public float MaxSpeed = 100;        
        public float RotationSpeed = 0;        
        public CollisionDetectionType CollisionDetection = CollisionDetectionType.CircleCast;
        public bool BounceOffSurfaces = true;        
        public bool CullProjectilesOutsideCameraBounds = true;
        public bool IsFixedTimestep = true;
        [ConditionalField(nameof(IsFixedTimestep)), Range(0.01f, 0.02f)] public float FixedTimestepRate = 0.01f;      

        [Foldout("Outline", true)]
        public bool DrawOutlines;
        [ConditionalField(nameof(DrawOutlines)), Range(0.0f, 1f)] public float OutlineSize;
        [ConditionalField(nameof(DrawOutlines))] public Gradient OutlineColor;

        [Foldout("Modifiers", true)]
        public Vector2 Gravity = Vector2.zero;
        [Range(0.0f, 1f)] public float BounceAbsorbtionY;
        [Range(0.0f, 1f)] public float BounceAbsorbtionX;                  
        [Range(-10f, 10f)] public float Acceleration = 0;

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
        protected int[] PreviousActiveProjectileIndexes;
        protected int ActiveProjectileIndexesPosition;

        public void Awake()
        {
            Interval = CoolOffTime + 0.25f;      // Start with a delay to allow time for scene to load
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

            ActiveProjectileIndexes = new int[size+1];
            PreviousActiveProjectileIndexes = new int[size+1];
        }

        public void UpdateEmitter(float tick)
        {
            if (AutoFire)
            {
                Interval -= tick;
            }
            else
            {
                // not autofire, so just deal with the cooldown
                if (Interval > 0)
                {
                    Interval -= tick;
                }
            }


            if (IsFixedTimestep)
            {
                if (AutoFire)
                {
                    // Interval has expired
                    while (Interval <= 0)
                    {
                        Interval += CoolOffTime;
                        // Fixed timestep, we must wait until next fixed frame to fire projectile
                        ProjectilesWaiting++;
                    }
                }

                bool updateExecuted = false;
                Timer += tick;

                // fixed timestep timer internal loop
                while (Timer > FixedTimestepRate)
                {
                    Timer -= FixedTimestepRate;
                    UpdateProjectiles(FixedTimestepRate);   // Must call UpdateProjectiles before firing new projectiles

                    if (AutoFire)
                    {
                        while (ProjectilesWaiting > 0)
                        {
                            ProjectilesWaiting--;
                            FireProjectile(Direction, ProjectilesWaiting * FixedTimestepRate);
                        }
                    }

                    updateExecuted = true;
                }

                // Update the data buffers
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

                if (AutoFire)
                {
                    while (Interval <= 0)
                    {
                        float leakedTime = Mathf.Abs(Interval);
                        Interval += CoolOffTime;
                        FireProjectile(Direction, leakedTime);
                    }
                }
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

        protected virtual void UpdateProjectiles(float tick)
        {

            ActiveProjectileCount = 0;
            ActiveOutlineCount = 0;

            //Update camera planes if needed
            if (CullProjectilesOutsideCameraBounds)
            {
                GeometryUtility.CalculateFrustumPlanes(Camera, Planes);
            }

            int previousIndexCount = ActiveProjectileIndexesPosition;
            ActiveProjectileIndexesPosition = 0;

            // Only loop through currently active projectiles
            for (int i = 0; i < PreviousActiveProjectileIndexes.Length - 1; i++)
            {
                // End of array is set to -1
                if (PreviousActiveProjectileIndexes[i] == -1)
                    break;

                Pool<ProjectileData>.Node node = Projectiles.GetActive(PreviousActiveProjectileIndexes[i]);
                UpdateProjectile(ref node, tick);

                // If still active store in our active projectile collection
                if (node.Active)
                {
                    ActiveProjectileIndexes[ActiveProjectileIndexesPosition] = node.NodeIndex;
                    ActiveProjectileIndexesPosition++;
                }
            }

            // Set end point of array so we know when to stop looping
            ActiveProjectileIndexes[ActiveProjectileIndexesPosition] = -1;

            // Overwrite old previous active projectile index array
            System.Array.Copy(ActiveProjectileIndexes, PreviousActiveProjectileIndexes, Mathf.Max(ActiveProjectileIndexesPosition, previousIndexCount));
        }

        protected virtual void UpdateProjectileColor(ref ProjectileData data)
        {
            data.Color = Color.Evaluate(1 - data.TimeToLive / TimeToLive);
            if (data.Outline.Item != null)
            {
                data.Outline.Item.Color = OutlineColor.Evaluate(1 - data.TimeToLive / TimeToLive);
            }
        }

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
                node.Active = false;
            }
        }

        public void ClearAllProjectiles()
        {
            for (int i = 0; i < ActiveProjectileIndexes.Length; i++)
            {
                // End of array is set to -1
                if (ActiveProjectileIndexes[i] == -1)
                    break;

                Pool<ProjectileData>.Node node = Projectiles.GetActive(ActiveProjectileIndexes[i]);
                ReturnNode(node);

                ActiveProjectileIndexes[i] = -1;
            }

            ActiveProjectileIndexesPosition = 0;
        }
        
        void OnDisable()
        {
            ClearAllProjectiles();
        }

    }
}