using UnityEngine;

namespace BulletHell
{
    // Used with ProjectileEmitterFlower to control spoke groupings
    public class EmitterGroup
    {
        public Vector2 Direction;
        public int SpokeCount;
        public float SpokeSpacing;
        public bool InvertRotation;

        public EmitterGroup(Vector2 direction, int spokeCount, float spokeSpacing, bool invertRotation)
        {
            Set(direction, spokeCount, spokeSpacing, invertRotation);
        }

        public void Set(Vector2 direction, int spokeCount, float spokeSpacing, bool invertRotation)
        {
            Direction = direction;
            SpokeCount = spokeCount;
            SpokeSpacing = spokeSpacing;
            InvertRotation = invertRotation;
        }
    }

}