using UnityEngine;

namespace BulletHell
{
    public class ProjectileEmitterMegamanGun : ProjectileEmitterBase
    {
        public new void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            // To allow for the enable / disable checkbox in Inspector
        }

        public override Pool<ProjectileData>.Node FireProjectile(Vector2 direction, float leakedTime)
        {
            Pool<ProjectileData>.Node node = new Pool<ProjectileData>.Node();

            node = Projectiles.Get();

            node.Item.Position = transform.position;
            node.Item.Speed = Speed;
            node.Item.Scale = Scale;
            node.Item.TimeToLive = TimeToLive;
            node.Item.Gravity = Gravity;
            node.Item.Color = Color.Evaluate(0);
            node.Item.Acceleration = Acceleration;

            node.Item.Velocity = Speed * Direction.normalized;

            // Setup outline if we have one
            if (ProjectilePrefab.Outline != null && DrawOutlines)
            {
                Pool<ProjectileData>.Node outlineNode = ProjectileOutlines.Get();

                outlineNode.Item.Position = node.Item.Position;
                outlineNode.Item.Scale = node.Item.Scale + OutlineSize;
                outlineNode.Item.Color = OutlineColor.Evaluate(0);

                node.Item.Outline = outlineNode;
            }

            // Keep track of active projectiles                       
            PreviousActiveProjectileIndexes[ActiveProjectileIndexesPosition] = node.NodeIndex;
            ActiveProjectileIndexesPosition++;
            if (ActiveProjectileIndexesPosition < ActiveProjectileIndexes.Length)
            {
                PreviousActiveProjectileIndexes[ActiveProjectileIndexesPosition] = -1;
            }
            else
            {
                Debug.Log("Error: Projectile was fired before list of active projectiles was refreshed.");
            }

            UpdateProjectile(ref node, leakedTime);

            return node;
        }

        protected override void UpdateProjectile(ref Pool<ProjectileData>.Node node, float tick)
        {
            if (node.Active)
            {
                node.Item.TimeToLive -= tick;

                // Projectile is active
                if (node.Item.TimeToLive > 0)
                {
                    // apply acceleration
                    node.Item.Velocity *= (1 + node.Item.Acceleration * tick);
                    // Gravity                    
                    node.Item.Velocity += node.Item.Gravity * tick;

                    // calculate where projectile will be at the end of this frame
                    Vector2 deltaPosition = node.Item.Velocity * tick;
                    float distance = deltaPosition.magnitude;

                    // If flag set - return projectiles that are no longer in view 
                    if (CullProjectilesOutsideCameraBounds)
                    {
                        Bounds bounds = new Bounds(node.Item.Position, new Vector3(node.Item.Scale, node.Item.Scale, node.Item.Scale));
                        if (!GeometryUtility.TestPlanesAABB(Planes, bounds))
                        {
                            ReturnNode(node);
                            return;
                        }
                    }

                    float radius = 0;
                    if (node.Item.Outline.Item != null)
                    {
                        radius = node.Item.Outline.Item.Scale / 2f;
                    }
                    else
                    {
                        radius = node.Item.Scale / 2f;
                    }

                    // Update foreground and outline color data
                    UpdateProjectileColor(ref node.Item);

                    int result = -1;
                    if (CollisionDetection == CollisionDetectionType.Raycast)
                    {
                        result = Physics2D.Raycast(node.Item.Position, deltaPosition, ContactFilter, RaycastHitBuffer, distance);
                    }
                    else if (CollisionDetection == CollisionDetectionType.CircleCast)
                    {
                        result = Physics2D.CircleCast(node.Item.Position, radius, deltaPosition, ContactFilter, RaycastHitBuffer, distance);
                    }

                    if (result > 0)
                    {
                        // Put whatever hit code you want here such as damage events

                        // Collision was detected, should we bounce off or destroy the projectile?
                        if (BounceOffSurfaces)
                        {
                            // Calculate the position the projectile is bouncing off the wall at
                            Vector2 projectedNewPosition = node.Item.Position + (deltaPosition * RaycastHitBuffer[0].fraction);
                            Vector2 directionOfHitFromCenter = RaycastHitBuffer[0].point - projectedNewPosition;
                            float distanceToContact = (RaycastHitBuffer[0].point - projectedNewPosition).magnitude;
                            float remainder = radius - distanceToContact;

                            // reposition projectile to the point of impact 
                            node.Item.Position = projectedNewPosition - (directionOfHitFromCenter.normalized * remainder);

                            // reflect the velocity for a bounce effect -- will work well on static surfaces
                            node.Item.Velocity = Vector2.Reflect(node.Item.Velocity, RaycastHitBuffer[0].normal);

                            // calculate remaining distance after bounce
                            deltaPosition = node.Item.Velocity * tick * (1 - RaycastHitBuffer[0].fraction);

                            // When gravity is applied, the positional change here is actually parabolic
                            node.Item.Position += deltaPosition;

                            // Absorbs energy from bounce
                            node.Item.Velocity = new Vector2(node.Item.Velocity.x * (1 - BounceAbsorbtionX), node.Item.Velocity.y * (1 - BounceAbsorbtionY));

                            //handle outline
                            if (node.Item.Outline.Item != null)
                            {
                                node.Item.Outline.Item.Position = node.Item.Position;
                            }
                        }
                        else
                        {
                            ReturnNode(node);
                        }
                    }
                    else
                    {
                        //No collision -move projectile
                        node.Item.Position += deltaPosition;
                        UpdateProjectileColor(ref node.Item);

                        // Update outline position
                        if (node.Item.Outline.Item != null)
                        {
                            node.Item.Outline.Item.Position = node.Item.Position;
                        }
                    }
                }
                else
                {
                    // End of life - return to pool
                    ReturnNode(node);
                }
            }
        }

    }

}
