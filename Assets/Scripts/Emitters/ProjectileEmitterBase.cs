using UnityEngine;

public enum CollisionDetectionType
{
    Raycast,
    CircleCast
};

public abstract class ProjectileEmitterBase : MonoBehaviour
{
    private Mesh Mesh;
    private Material Material;

    public float TimeToLive = 5;
    public Vector2 Direction = Vector2.up;
    public Vector2 Gravity = Vector2.zero;
    public Vector2 BounceAbsorbtion = Vector2.zero;
    public Gradient Color;

    [Range(0.001f, 5f)]
    public float INTERVAL = 0.1f;
    private float Interval;

    [Range(0.001f, 10f)]
    public float Speed = 1;

    [Range(-10f, 10f)]
    public float Acceleration = 0;

    [Range(0.01f, 2f)]
    public float Scale = 0.05f;
    
    public float RotationSpeed = 0;
    public bool AutoFire = true;
    public bool BounceOffSurfaces = true;
    public ProjectileType ProjectileType;
    public bool CullProjectilesOutsideCameraBounds = true;
    public CollisionDetectionType CollisionDetection = CollisionDetectionType.CircleCast;

    //If set to -1 -- projectile pool size will be auto-calculated
    public int ProjectilePoolSize = -1;
    // Each emitter has its own ProjectileData pool
    protected Pool<ProjectileData> Projectiles;
   
    // Collision layer
    private int LayerMask = 1;        
    private RaycastHit2D[] RaycastHitBuffer = new RaycastHit2D[1];

    // Current active projectiles from this emitter
    public int ActiveProjectileCount { get; private set; }

    // For cull check
    Plane[] Planes = new Plane[6];
    private Camera Camera;

    public void Start()
    {
        Camera = Camera.main;

        Interval = INTERVAL;

        // If projectile type is not set, use default
        if (ProjectileType == null)
            ProjectileType = ProjectileManager.Instance.GetProjectileType(0);     
    }

    public void Initialize(int size)
    {
        Projectiles = new Pool<ProjectileData>(size);
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
 
    private void UpdateProjectiles(float tick)
    {
        ActiveProjectileCount = 0;

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

                    // apply gravity
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
                            Projectiles.Nodes[i].Item.TimeToLive = -1;
                            Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                        }
                    }

                    int result = -1;
                    if (CollisionDetection == CollisionDetectionType.Raycast)
                    {
                        result = Physics2D.Raycast(Projectiles.Nodes[i].Item.Position, deltaPosition, contactFilter, RaycastHitBuffer, distance);
                    }
                    else if (CollisionDetection == CollisionDetectionType.CircleCast)
                    {
                        result = Physics2D.CircleCast(Projectiles.Nodes[i].Item.Position, Projectiles.Nodes[i].Item.Scale / 2f, Projectiles.Nodes[i].Item.Velocity, contactFilter, RaycastHitBuffer, distance);
                        if (result > 0 && RaycastHitBuffer[0].distance == 0)
                        {
                            result = -1;
                        }                        
                    }


                    if (result > 0)
                    {
                        // Put whatever hit code you want here such as damage events

                        // Collision was detected, should we bounce off or destroy the projectile?
                        if (BounceOffSurfaces)
                        {
                            // rudementary bounce -- will work well on static surfaces
                            Projectiles.Nodes[i].Item.Velocity = Vector2.Reflect(Projectiles.Nodes[i].Item.Velocity, RaycastHitBuffer[0].normal);

                            // what fraction of the distance do we still have to move this frame?
                            float leakedFraction = 1f - RaycastHitBuffer[0].distance / distance;

                            deltaPosition = Projectiles.Nodes[i].Item.Velocity * tick * leakedFraction;
                            Projectiles.Nodes[i].Item.Position = RaycastHitBuffer[0].centroid + deltaPosition;
                            Projectiles.Nodes[i].Item.Color = Color.Evaluate(1 - Projectiles.Nodes[i].Item.TimeToLive / TimeToLive);

                            // Absorbs energy from bounce
                            Projectiles.Nodes[i].Item.Velocity = new Vector2(Mathf.Lerp(Projectiles.Nodes[i].Item.Velocity.x, 0, BounceAbsorbtion.x), Mathf.Lerp(Projectiles.Nodes[i].Item.Velocity.y, 0, BounceAbsorbtion.y));

                            projectileManager.UpdateBufferData(ActiveProjectileCount, ProjectileType, Projectiles.Nodes[i].Item);

                            ActiveProjectileCount++;
                        }
                        else
                        {
                            Projectiles.Nodes[i].Item.TimeToLive = -1;
                            Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                        }
                    }
                    else
                    {
                        //No collision -move projectile
                        Projectiles.Nodes[i].Item.Position += deltaPosition;
                        Projectiles.Nodes[i].Item.Color = Color.Evaluate(1 - Projectiles.Nodes[i].Item.TimeToLive / TimeToLive);

                        projectileManager.UpdateBufferData(ActiveProjectileCount, ProjectileType, Projectiles.Nodes[i].Item);

                        ActiveProjectileCount++;
                    }
                }
                else
                {
                    // End of life - return to pool
                    Projectiles.Return(Projectiles.Nodes[i].NodeIndex);
                }
            }
        }
    }

    public void ClearAllProjectiles()
    {
        for (int i = 0; i < Projectiles.Nodes.Length; i++)
        {
            if (Projectiles.Nodes[i].Active)
            {               
                Projectiles.Nodes[i].Item.TimeToLive = -1;
                Projectiles.Return(Projectiles.Nodes[i].NodeIndex);                
            }
        }
    }

}
