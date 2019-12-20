using UnityEngine;

namespace BulletHell
{
    // Used with ProjectileEmitterFlower to control spoke groupings
    public class EmitterGroup
    {
        public Vector2 Direction;
        public int SpokeCount;
        public float SpokeSpacing;

        public EmitterGroup(Vector2 direction, int spokeCount, float spokeSpacing)
        {
            Direction = direction;
            SpokeCount = spokeCount;
            SpokeSpacing = spokeSpacing;
        }
    }

}