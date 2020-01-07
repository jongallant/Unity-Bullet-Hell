using UnityEngine;

namespace BulletHell
{
    public class ProjectileEmitterAdvanced : ProjectileEmitterBase
    {
        [Range(1, 10)]
        public int GroupCount = 1;
        private EmitterGroup[] Groups;

        [Range(1, 10)]
        public int SpokeCount = 3;

        [Range(1, 100)]
        public float SpokeSpacing = 40;

        private int LastGroupCountPoll = -1;

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

            if (Groups == null || LastGroupCountPoll != GroupCount)
            {
                float rotation = 0;
                for (int n = 0; n < Groups.Length; n++)
                {
                    if (n < GroupCount && Groups[n] == null)
                    {
                        Groups[n] = new EmitterGroup(Rotate(Direction, rotation).normalized, SpokeCount, SpokeSpacing);
                    }
                    else if (n < GroupCount)
                    {
                        Groups[n].Direction = Rotate(Direction, rotation).normalized;
                        Groups[n].SpokeCount = SpokeCount;
                        Groups[n].SpokeSpacing = SpokeSpacing;
                    }
                    else
                    {
                        //n is greater than GroupCount -- ensure we clear the rest of the buffer
                        Groups[n] = null;
                    }
                    rotation += 360 / GroupCount;
                }
                LastGroupCountPoll = GroupCount;
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
                    rotation += 360 / GroupCount;
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
                            node.Item.Scale = Scale;
                            node.Item.TimeToLive = TimeToLive - leakedTime;
                            node.Item.Direction = Rotate(direction, rotation).normalized;
                            node.Item.Gravity = Gravity;
                            node.Item.Velocity = Speed * Rotate(Groups[g].Direction, rotation).normalized;
                            node.Item.Position += node.Item.Velocity * leakedTime;
                            node.Item.Color = new Color(0.6f, 0.7f, 0.6f, 1);
                            node.Item.Acceleration = Acceleration;
                            rotation += SpokeSpacing;
                        }
                        else
                        {
                            node.Item.Position = transform.position;
                            node.Item.Scale = Scale;
                            node.Item.TimeToLive = TimeToLive - leakedTime;
                            node.Item.Direction = Rotate(direction, rotation).normalized;
                            node.Item.Gravity = Gravity;
                            node.Item.Velocity = Speed * Rotate(Groups[g].Direction, -rotation).normalized;
                            node.Item.Position += node.Item.Velocity * leakedTime;
                            node.Item.Color = new Color(0.6f, 0.7f, 0.6f, 1);
                            node.Item.Acceleration = Acceleration;
                        }

                        left = !left;
                    }

                    Groups[g].Direction = Rotate(Groups[g].Direction, RotationSpeed);
                }
            }
        }

        public new void UpdateEmitter()
        {
            base.UpdateEmitter();
        }
    }
}