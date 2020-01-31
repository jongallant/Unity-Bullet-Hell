using System;
using UnityEngine;

namespace BulletHell
{
    public class ProjectilePrefab : MonoBehaviour
    {
        public int Index { get; protected set; }          // To identify the ProjectilePrefab
        public int BufferIndex { get; protected set; }    // To identify which computebuffer is used for this type

        public Texture Texture;
        public Mesh Mesh;
        public float ZIndez;
        public bool PixelSnap;

        [SerializeField] protected bool StaticColor = false;
        [ConditionalField(nameof(StaticColor)), SerializeField] protected Color Color;
        [SerializeField, Range(1, 1000000)] private int MaxProjectileCount;
        
        public bool IsStaticColor { get { return StaticColor; } }
        public Material Material { get; protected set; }        

        public ProjectilePrefab Outline;
        
        public void Initialize(int index)
        {
            Index = index;

            // If no value set for MaxProjectiles default to 50000
            if (MaxProjectileCount <= 0)
                MaxProjectileCount = 50000;

            if (StaticColor)
                Material = new Material(Shader.Find("ProjectileShader_StaticColor"));
            else
                Material = new Material(Shader.Find("ProjectileShader"));

            Material.enableInstancing = true;
            Material.SetColor("_Color", Color);
            Material.SetFloat("_ZIndex", ZIndez);
            Material.SetFloat("PixelSnap", Convert.ToSingle(PixelSnap));

            if (Texture != null)
                Material.SetTexture("_MainTex", Texture);
        }

        public void IncrementBufferIndex()
        {
            BufferIndex++;
        }

        public void ResetBufferIndex()
        {
            BufferIndex = 0;
        }

        public int GetMaxProjectileCount()
        {
            return MaxProjectileCount;
        }
    }
}
