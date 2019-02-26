using UnityEngine;

public class ProjectileEmitterAdvanced : ProjectileEmitterBase
{
    [SerializeField]
    [Range(1, 10)]
    protected int GroupCount = 1;
    private EmitterGroup[] Groups;

    [SerializeField]
    [Range(1, 10)]
    protected int SpokeCount = 3;

    [SerializeField]
    [Range(1, 100)]
    protected float SpokeSpacing = 40;
         
    public new void Start()
    {
        base.Start();
        RefreshGroups();
    }

    private void RefreshGroups()
    {
        if (Groups == null || Groups.Length != GroupCount)
        {
            Groups = new EmitterGroup[GroupCount];
            Vector2 direction = Direction;
            float rotation = 0;

            for (int n = 0; n < Groups.Length; n++)
            {
                Groups[n] = new EmitterGroup(Rotate(direction, rotation).normalized, SpokeCount, SpokeSpacing);
                rotation += 360 / Groups.Length;
            }
        }
    }
    
    protected override void FireProjectile(Vector2 direction, float leakedTime)
    {
        //rebuild groups if value was changed (creates garbage - only for debug)
        if (GroupCount != Groups.Length)
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
                        node.Item.TimeToLive = TimeToLive;
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
                        node.Item.TimeToLive = TimeToLive;
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
