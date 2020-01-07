using UnityEngine;

namespace BulletHell
{
    public class ProjectileType : MonoBehaviour
    {
        public int Index { get; private set; }
        public int BufferIndex;
        public Mesh Mesh;
        public Material Material;
        public int MaxProjectileCount;
        public int ActiveProjectileCount;

        public void SetIndex(int index)
        {
            Index = index;
        }
    }
}