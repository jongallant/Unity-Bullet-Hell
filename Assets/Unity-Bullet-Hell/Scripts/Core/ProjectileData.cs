using UnityEngine;

namespace BulletHell
{   
    public class ProjectileData
    {
        public Vector2 Velocity;
        public float Acceleration;
        public Vector2 Gravity;
        public Vector2 Position;
        public float Rotation;
        public Color Color;
        public float Scale;
        public float TimeToLive;
        public float Speed;

        public ColorPulse Pulse;
        public ColorPulse OutlinePulse;

        public Transform Target;
        public bool FollowTarget;
        public float FollowIntensity;

        // Stores the pooled node that is used to draw the shadow for this projectile
        public Pool<ProjectileData>.Node Outline;
    }
}