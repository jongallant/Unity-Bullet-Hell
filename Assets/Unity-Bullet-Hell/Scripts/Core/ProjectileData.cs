using UnityEngine;

namespace BulletHell
{   
    public class ProjectileData
    {
        public Vector2 Direction;
        public Vector2 Velocity;
        public float Acceleration;
        public Vector2 Gravity;
        public Vector2 Position;
        public float Rotation;
        public Color Color;
        public float Scale;
        public float TimeToLive;

        public bool PulseDown;
        public float PulseTime;

        // Stores the pooled node that is used to draw the shadow for this projectile
        public Pool<ProjectileData>.Node Outline;
        public bool OutlinePulseDown;
        public float OutlinePulseTime;
    }
}