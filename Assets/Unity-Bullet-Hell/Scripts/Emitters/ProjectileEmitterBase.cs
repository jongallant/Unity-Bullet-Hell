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
        [Range(0.001f, 5f), SerializeField] protected float INTERVAL = 0.1f;
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
        
        public void Awake()
        {
            Camera = Camera.main;            
        }

        public void Start()
        {
            Interval = INTERVAL;

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
        }

        public void UpdateEmitter()
        {
            if (AutoFire)
            {
                Interval -= Time.deltaTime;
            }
            UpdateProjectiles(Time.deltaTime);
        }

        public void ResolveLeakedTime()
        {
            if (AutoFire)
            {
                // Spawn in new projectiles for next frame
                while (Interval <= 0)
                {
                    float leakedTime = Mathf.Abs(Interval);
                    Interval += INTERVAL;
                    FireProjectile(Direction, leakedTime);
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

        public abstract void FireProjectile(Vector2 direction, float leakedTime);

        protected virtual void UpdateProjectiles(float tick)
        {
            ActiveProjectileCount = 0;
            ActiveOutlineCount = 0;

            ContactFilter2D contactFilter = new ContactFilter2D
            {
                layerMask = LayerMask,
                useTriggers = false,
            };

            ProjectileManager projectileManager = ProjectileManager.Instance;

            //Update camera planes if needed
            if (CullProjectilesOutsideCameraBounds)
            {
                GeometryUtility.CalculateFrustumPlanes(Camera, Planes);
            }

            // loop through all active projectile data
            for (int i = 0; i < Projectiles.Nodes.Length; i++)
            {
                if (Projectiles.Nodes[i].Active)
                {
                    Projectiles.Nodes[i].Item.TimeToLive -= tick;

                    // Projectile is active
                    if (Projectiles.Nodes[i].Item.TimeToLive > 0)
                    {
                        // apply acceleration
                        Projectiles.Nodes[i].Item.Velocity *= (1 + Projectiles.Nodes[i].Item.Acceleration * tick);

                        // apply half gravity before -- and other half after the position update to attempt to smooth out the interpolation
                        Projectiles.Nodes[i].Item.Velocity += Projectiles.Nodes[i].Item.Gravity * tick;                       

                        // calculate where projectile will be at the end of this frame
                        Vector2 deltaPosition = Projectiles.Nodes[i].Item.Velocity * tick;
                        float distance = deltaPosition.magnitude;

                        // If flag set - return projectiles that are no longer in view 
                        if (CullProjectilesOutsideCameraBounds)
                        {
                            Bounds bounds = new Bounds(Projectiles.Nodes[i].Item.Position, new Vector3(Projectiles.Nodes[i].Item.Scale, Projectiles.Nodes[i].Item.Scale, Projectiles.Nodes[i].Item.Scale));
                            if (!GeometryUtility.TestPlanesAABB(Planes, bounds))
                            {
                                ReturnNode(Projectiles.Nodes[i]);
                            }
                        }

                        float radius = 0;
                        if (Projectiles.Nodes[i].Item.Outline.Item != null)
                        {
                            radius = Projectiles.Nodes[i].Item.Outline.Item.Scale / 2f;
                        }
                        else
                        {
                            radius = Projectiles.Nodes[i].Item.Scale / 2f;
                        }

                        int result = -1;
                        if (CollisionDetection == CollisionDetectionType.Raycast)
                        {
                            result = Physics2D.Raycast(Projectiles.Nodes[i].Item.Position, deltaPosition, contactFilter, RaycastHitBuffer, distance);
                        }
                        else if (CollisionDetection == CollisionDetectionType.CircleCast)
                        {
                            result = Physics2D.CircleCast(Projectiles.Nodes[i].Item.Position, radius, deltaPosition, contactFilter, RaycastHitBuffer, distance);
                        }

                        if (result > 0)
                        {
                            // Put whatever hit code you want here such as damage events

                            // Collision was detected, should we bounce off or destroy the projectile?
                            if (BounceOffSurfaces)
                            {
                                // Calculate the position the projectile is bouncing off the wall at
                                Vector2 projectedNewPosition = Projectiles.Nodes[i].Item.Position + (deltaPosition * RaycastHitBuffer[0].fraction);
                                Vector2 directionOfHitFromCenter = RaycastHitBuffer[0].point - projectedNewPosition;
                                float distanceToContact = (RaycastHitBuffer[0].point - projectedNewPosition).magnitude;
                                float remainder = radius - distanceToContact;

                                // reposition projectile to the point of impact 
                                Projectiles.Nodes[i].Item.Position = projectedNewPosition - (directionOfHitFromCenter.normalized * remainder);

                                // reflect the velocity for a bounce effect -- will work well on static surfaces
                                Projectiles.Nodes[i].Item.Velocity = Vector2.Reflect(Projectiles.Nodes[i].Item.Velocity, RaycastHitBuffer[0].normal);

                                // calculate remaining distance after bounce
                                deltaPosition = Projectiles.Nodes[i].Item.Velocity * tick * (1 - RaycastHitBuffer[0].fraction);

                                Projectiles.Nodes[i].Item.Position += deltaPosition;

                                // Absorbs energy from bounce
                                Projectiles.Nodes[i].Item.Velocity = new Vector2(Projectiles.Nodes[i].Item.Velocity.x * (1 - BounceAbsorbtionX), Projectiles.Nodes[i].Item.Velocity.y * (1 - BounceAbsorbtionY));

                                //handle outline
                                if (Projectiles.Nodes[i].Item.Outline.Item != null)
                                {
                                    Projectiles.Nodes[i].Item.Outline.Item.Position = Projectiles.Nodes[i].Item.Position;
                                    projectileManager.UpdateBufferData(ProjectilePrefab.Outline, Projectiles.Nodes[i].Item.Outline.Item);
                                    ActiveOutlineCount++;
                                }

                                projectileManager.UpdateBufferData(ProjectilePrefab, Projectiles.Nodes[i].Item);
                                ActiveProjectileCount++;
                            }
                            else
                            {
                                ReturnNode(Projectiles.Nodes[i]);
                            }
                        }
                        else
                        {
                            //No collision -move projectile
                            Projectiles.Nodes[i].Item.Position += deltaPosition;

                            //handle outline
                            if (Projectiles.Nodes[i].Item.Outline.Item != null)
                            {
                                Projectiles.Nodes[i].Item.Outline.Item.Position = Projectiles.Nodes[i].Item.Position;
                                projectileManager.UpdateBufferData(ProjectilePrefab.Outline, Projectiles.Nodes[i].Item.Outline.Item);
                                ActiveOutlineCount++;
                            }

                            projectileManager.UpdateBufferData(ProjectilePrefab, Projectiles.Nodes[i].Item);
                            ActiveProjectileCount++;
                        }
                    }
                    else
                    {
                        // End of life - return to pool
                        ReturnNode(Projectiles.Nodes[i]);
                    }
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