using UnityEngine;

namespace BulletHell
{
    public enum FollowTargetType
    {
        Homing,
        LockOnShot
    };

    public class ProjectileEmitterAdvanced : ProjectileEmitterBase
    {
        ColorPulse StaticOutlinePulse;
        ColorPulse StaticPulse;

        [Foldout("General", true)]
        [SerializeField] public bool UseColorPulse;
        [ConditionalField(nameof(UseColorPulse)), SerializeField] protected float PulseSpeed;
        [ConditionalField(nameof(UseColorPulse)), SerializeField] protected bool UseStaticPulse;

        [Foldout("Spokes", true)]
        [Range(1, 10), SerializeField] protected int GroupCount = 1;
        [Range(0, 1), SerializeField] protected float GroupSpacing = 40;
        [Range(1, 10), SerializeField] protected int SpokeCount = 3;
        [Range(0, 100), SerializeField] protected float SpokeSpacing = 40;
        [SerializeField] protected bool MirrorPairRotation;                                                     
        [ConditionalField(nameof(MirrorPairRotation)), SerializeField] protected bool PairGroupDirection;       

        [Foldout("Modifiers", true)]
        [SerializeField] public bool UseFollowTarget;       
        [ConditionalField(nameof(UseFollowTarget)), SerializeField] protected Transform Target;
        [ConditionalField(nameof(UseFollowTarget)), SerializeField] protected FollowTargetType FollowTargetType = FollowTargetType.Homing;
        [ConditionalField(nameof(UseFollowTarget)), Range(0, 5), SerializeField] protected float FollowIntensity;

        [Foldout("Outline", true)]
        [SerializeField] protected bool UseOutlineColorPulse;
        [ConditionalField(nameof(UseOutlineColorPulse)), SerializeField] protected float OutlinePulseSpeed;
        [ConditionalField(nameof(UseOutlineColorPulse)), SerializeField] protected bool UseOutlineStaticPulse;

        private EmitterGroup[] Groups;
        private int LastGroupCountPoll = -1;
        private bool PreviousMirrorPairRotation = false;
        private bool PreviousPairGroupDirection = false;

        public new void Awake()
        {
            base.Awake();
            Groups = new EmitterGroup[10];
        }

        public new void Start()
        {
            base.Start();
            RefreshGroups();
        }

        private void RefreshGroups()
        {
            if (GroupCount > 10)
            {
                Debug.Log("Max Group Count is set to 10.  You attempted to set it to " + GroupCount.ToString() + ".");
                return;
            }

            bool mirror = false;
            if (Groups == null || LastGroupCountPoll != GroupCount || PreviousMirrorPairRotation != MirrorPairRotation || PreviousPairGroupDirection != PairGroupDirection)
            {               
                // Refresh the groups, they were changed
                float rotation = 0;
                for (int n = 0; n < Groups.Length; n++)
                {
                    if (n < GroupCount && Groups[n] == null)
                    {
                        Groups[n] = new EmitterGroup(Rotate(Direction, rotation).normalized, SpokeCount, SpokeSpacing, mirror);
                    }
                    else if (n < GroupCount)
                    {
                        Groups[n].Set(Rotate(Direction, rotation).normalized, SpokeCount, SpokeSpacing, mirror);
                    }
                    else
                    {
                        //n is greater than GroupCount -- ensure we clear the rest of the buffer
                        Groups[n] = null;
                    }

                    // invert the mirror flag if needed
                    if (MirrorPairRotation)
                        mirror = !mirror;

                    // sets the starting direction of all the groups so we divide by 360 to evenly distribute their direction
                    // Could reduce the scope of the directions here
                    rotation = CalculateGroupRotation(n, rotation);
                }
                LastGroupCountPoll = GroupCount;
                PreviousMirrorPairRotation = MirrorPairRotation;
                PreviousPairGroupDirection = PairGroupDirection;
            }
            else if (RotationSpeed == 0)
            {
                float rotation = 0;
                // If rotation speed is locked, then allow to update Direction of groups
                for (int n = 0; n < Groups.Length; n++)
                {
                    if (Groups[n] != null)
                    {
                        Groups[n].Direction = Rotate(Direction, rotation).normalized;
                    }

                    rotation = CalculateGroupRotation(n, rotation);
                }
            }
        }

        public override void FireProjectile(Vector2 direction, float leakedTime)
        {
            RefreshGroups();

            for (int g = 0; g < GroupCount; g++)
            {
                if (Projectiles.AvailableCount > SpokeCount)
                {
                    float rotation = 0;
                    bool left = true;

                    for (int n = 0; n < SpokeCount; n++)
                    {
                        Pool<ProjectileData>.Node node = Projectiles.Get();

                        if (left)
                        {
                            node.Item.Position = transform.position;
                            node.Item.Speed = Speed;
                            node.Item.Scale = Scale;
                            node.Item.TimeToLive = TimeToLive - leakedTime;
                            node.Item.Gravity = Gravity;
                            if (UseFollowTarget && FollowTargetType == FollowTargetType.LockOnShot && Target != null)
                            {
                                Groups[g].Direction = (Target.transform.position - transform.position).normalized;
                            }
                            node.Item.Velocity = Speed * Rotate(Groups[g].Direction, rotation).normalized;
                            node.Item.Position += node.Item.Velocity * leakedTime;
                            node.Item.Color = Color.Evaluate(0);
                            node.Item.Acceleration = Acceleration;
                            node.Item.FollowTarget = UseFollowTarget;
                            node.Item.FollowIntensity = FollowIntensity;
                            node.Item.Target = Target;
                            rotation += SpokeSpacing;
                        }
                        else
                        {
                            node.Item.Position = transform.position;
                            node.Item.Scale = Scale;
                            node.Item.Speed = Speed;
                            node.Item.TimeToLive = TimeToLive - leakedTime;
                            node.Item.Gravity = Gravity;
                            if (UseFollowTarget && FollowTargetType == FollowTargetType.LockOnShot && Target != null)
                            {
                                Groups[g].Direction = (Target.transform.position - transform.position).normalized;
                            }
                            node.Item.Velocity = Speed * Rotate(Groups[g].Direction, -rotation).normalized;
                            node.Item.Position += node.Item.Velocity * leakedTime;
                            node.Item.Color = Color.Evaluate(0);
                            node.Item.Acceleration = Acceleration;
                            node.Item.FollowTarget = UseFollowTarget;
                            node.Item.FollowIntensity = FollowIntensity;
                            node.Item.Target = Target;
                        }

                        if (ProjectilePrefab.Outline != null && DrawOutlines)
                        {
                            Pool<ProjectileData>.Node outlineNode = ProjectileOutlines.Get();

                            outlineNode.Item.Position = node.Item.Position;
                            outlineNode.Item.Scale = node.Item.Scale + OutlineSize;
                            outlineNode.Item.TimeToLive = node.Item.TimeToLive;
                            outlineNode.Item.Gravity = node.Item.Gravity;
                            outlineNode.Item.Velocity = node.Item.Velocity;
                            outlineNode.Item.Position = node.Item.Position;
                            outlineNode.Item.Color = OutlineColor.Evaluate(0);
                            outlineNode.Item.Acceleration = node.Item.Acceleration;
                            
                            node.Item.Outline = outlineNode;
                        }

                        left = !left;
                    }


                    if (Groups[g].InvertRotation)
                        Groups[g].Direction = Rotate(Groups[g].Direction, -RotationSpeed);
                    else
                        Groups[g].Direction = Rotate(Groups[g].Direction, RotationSpeed);
                }
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, Scale);

            Gizmos.color = UnityEngine.Color.yellow;

            float rotation = 0;

            for (int n = 0; n < GroupCount; n++)
            {
                Vector2 direction = Rotate(Direction, rotation).normalized * (Scale + 0.2f);
                Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y) + direction);

                rotation = CalculateGroupRotation(n, rotation);
            }

            Gizmos.color = UnityEngine.Color.red;
            rotation = 0;
            float spokeRotation = 0;
            bool left = true;
            for (int n = 0; n < GroupCount; n++)
            {
                Vector2 groupDirection = Rotate(Direction, rotation).normalized;
                spokeRotation = 0;
                left = true;

                for (int m = 0; m < SpokeCount; m++)
                {
                    Vector2 direction = Vector2.zero;
                    if (left)
                    {
                        direction = Rotate(groupDirection, spokeRotation).normalized * (Scale + 0.15f);
                        spokeRotation += SpokeSpacing;
                    }
                    else
                    {
                        direction = Rotate(groupDirection, -spokeRotation).normalized * (Scale + 0.15f);
                    }
                    Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y) + direction);

                    left = !left;
                }

                rotation = CalculateGroupRotation(n, rotation);
            }
        }

        private float CalculateGroupRotation(int index, float currentRotation)
        {
            if (PairGroupDirection)
            {
                if (index % 2 == 1)
                    currentRotation += 360 * GroupSpacing * 2f / GroupCount;
            }
            else
            {
                currentRotation += 360 * GroupSpacing / GroupCount;
            }
            return currentRotation;
        }

        // There is code duplication here, instead of calling base update.  this prevents from having to loop the projectiles twice.
        protected override void UpdateProjectiles(float tick)
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

            UpdateStaticPulses(tick);

            // loop through all active projectile data
            for (int i = 0; i < Projectiles.Nodes.Length; i++)
            {
                if (Projectiles.Nodes[i].Active)
                {
                    Projectiles.Nodes[i].Item.TimeToLive -= tick;

                    // Projectile is active
                    if (Projectiles.Nodes[i].Item.TimeToLive > 0)
                    {
                        UpdateProjectileNodePulse(tick, ref Projectiles.Nodes[i].Item);

                        // apply acceleration
                        Projectiles.Nodes[i].Item.Velocity *= (1 + Projectiles.Nodes[i].Item.Acceleration * tick);

                        // follow target
                        if (FollowTargetType == FollowTargetType.Homing && Projectiles.Nodes[i].Item.FollowTarget && Projectiles.Nodes[i].Item.Target != null)
                        {                         
                            Projectiles.Nodes[i].Item.Speed += Acceleration * tick;
                            Projectiles.Nodes[i].Item.Speed = Mathf.Clamp(Projectiles.Nodes[i].Item.Speed, -MaxSpeed, MaxSpeed);

                            Vector2 desiredVelocity = (new Vector2(Target.transform.position.x, Target.transform.position.y) - Projectiles.Nodes[i].Item.Position).normalized;
                            desiredVelocity *= Projectiles.Nodes[i].Item.Speed;

                            Vector2 steer = desiredVelocity - Projectiles.Nodes[i].Item.Velocity;
                            Projectiles.Nodes[i].Item.Velocity = Vector2.ClampMagnitude(Projectiles.Nodes[i].Item.Velocity + steer * Projectiles.Nodes[i].Item.FollowIntensity * tick, Projectiles.Nodes[i].Item.Speed);
                        }
                        else
                        {
                            // apply gravity
                            Projectiles.Nodes[i].Item.Velocity += Projectiles.Nodes[i].Item.Gravity * tick;
                        }

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

                        // Update foreground and outline color data
                        UpdateProjectileColor(ref Projectiles.Nodes[i].Item);

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

                            UpdateProjectileColor(ref Projectiles.Nodes[i].Item);

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

        public new void UpdateEmitter()
        {
            base.UpdateEmitter();
        }

        private void UpdateProjectileNodePulse(float tick, ref ProjectileData data)
        {
            if (UseColorPulse && !UseStaticPulse)
            {
                data.Pulse.Update(tick, PulseSpeed);
            }

            if (UseOutlineColorPulse && !UseOutlineStaticPulse)
            {
                data.OutlinePulse.Update(tick, OutlinePulseSpeed);
            }
        }

        private void UpdateStaticPulses(float tick)
        {
            //projectile pulse
            if (UseColorPulse && UseStaticPulse)
            {
                StaticPulse.Update(tick, PulseSpeed);
            }

            //outline pulse
            if (UseOutlineColorPulse && UseOutlineStaticPulse)
            {
                StaticOutlinePulse.Update(tick, OutlinePulseSpeed);
            }
        }

        private void UpdateProjectileColor(ref ProjectileData data)
        {
            // foreground
            if (UseColorPulse)
            {
                if (UseStaticPulse)
                {
                    data.Color = Color.Evaluate(StaticPulse.Fraction);
                }
                else
                {
                    data.Color = Color.Evaluate(data.Pulse.Fraction);
                }
            }
            else
            {
                data.Color = Color.Evaluate(1 - data.TimeToLive / TimeToLive);
            }

            //outline
            if (data.Outline.Item != null)
            {
                if (UseOutlineColorPulse)
                {
                    if (UseOutlineStaticPulse)
                    {
                        data.Outline.Item.Color = OutlineColor.Evaluate(StaticOutlinePulse.Fraction);
                    }
                    else
                    {
                        data.Outline.Item.Color = OutlineColor.Evaluate(data.OutlinePulse.Fraction);
                    }
                }
                else
                {
                    data.Outline.Item.Color = OutlineColor.Evaluate(1 - data.TimeToLive / TimeToLive);
                }
            }
        }

    }
}