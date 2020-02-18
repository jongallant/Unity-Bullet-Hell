using UnityEngine;

namespace BulletHell
{
    // Most basic emitter implementation
    public class ProjectileEmitterBasic : ProjectileEmitterBase
    {
        public override Pool<ProjectileData>.Node FireProjectile(Vector2 direction, float leakedTime)
        {
            Pool<ProjectileData>.Node node = Projectiles.Get();

            node.Item.Position = transform.position;
            node.Item.Scale = Scale;
            node.Item.TimeToLive = TimeToLive - leakedTime;
            node.Item.Velocity = Speed * Direction.normalized;
            node.Item.Position += node.Item.Velocity * leakedTime;
            node.Item.Color = new Color(0.6f, 0.7f, 0.6f, 1);
            node.Item.Acceleration = Acceleration;

            Direction = Rotate(Direction, RotationSpeed);
            
            return node;
        }

        protected override void UpdateProjectile(ref Pool<ProjectileData>.Node node, float tick)
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdateProjectiles(float tick)
        {
            throw new System.NotImplementedException();
        }
    }
}