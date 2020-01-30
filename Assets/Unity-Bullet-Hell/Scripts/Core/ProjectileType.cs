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

        public ProjectileType Border;

        public void Initialize(int index)
        {
            Index = index;
            Material = new Material(Shader.Find("ProjectileShader"));
            Material.enableInstancing = true;
            Material.SetFloat("_StaticColor", 0);
        }
    }
}