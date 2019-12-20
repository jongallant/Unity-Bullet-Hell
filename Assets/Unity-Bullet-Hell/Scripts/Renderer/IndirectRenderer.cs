using UnityEngine;

namespace BulletHell
{
    public class IndirectRenderer
    {
        private ComputeBuffer ArgsBuffer;
        private ComputeBuffer TransformBuffer;
        private ComputeBuffer ColorBuffer;

        // Data buffers
        private Vector4[] TransformData;
        private Vector4[] ColorData;

        // Projectile Details
        private Mesh Mesh;
        private Material Material;

        // When set true - color data not sent to gpu
        // If using a static color make sure to check this to true on the Material for performance boost.
        private bool StaticColor;

        // DrawMeshInstancedIndirect requires an args buffer
        // [index count per instance, instance count, start index location, base vertex location, start instance location]
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        public IndirectRenderer(int maxProjectiles, Material material, Mesh mesh, bool staticColor)
        {
            //Turn off v-sync
            QualitySettings.vSyncCount = 0;

            StaticColor = staticColor;
            Mesh = mesh;
            Material = material;

            ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            InitializeBuffers(maxProjectiles);
        }

        void InitializeBuffers(int maxProjectiles)
        {
            ReleaseBuffers(false);

            // Create Compute Buffers
            TransformBuffer = new ComputeBuffer(maxProjectiles, sizeof(float) * 4);
            if (!StaticColor)
                ColorBuffer = new ComputeBuffer(maxProjectiles, sizeof(float) * 4);

            // Create Data Buffers
            TransformData = new Vector4[maxProjectiles];
            if (!StaticColor)
                ColorData = new Vector4[maxProjectiles];

            // Set the active buffers on the material
            Material.SetBuffer("positionBuffer", TransformBuffer);
            if (!StaticColor)
                Material.SetBuffer("colorBuffer", ColorBuffer);

            // Update argument buffer
            uint numIndices = (Mesh != null) ? (uint)Mesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)maxProjectiles;
            ArgsBuffer.SetData(args);
        }

        public void UpdateBufferData(int index, ProjectileData data)
        {
            if (index < TransformData.Length)
            {
                TransformData[index] = new Vector4(data.Position.x, data.Position.y, data.Scale, data.Rotation);
                if (!StaticColor)
                    ColorData[index] = new Vector4(data.Color.r, data.Color.g, data.Color.b, data.Color.a);
            }
            else
            {
                Debug.Log("Error: Initialized more projectiles than Projectile Type allows.");
            }
        }

        public void Draw(int activeProjectileCount)
        {
            // Update our compute buffers with latest data 
            TransformBuffer.SetData(TransformData, 0, 0, activeProjectileCount);
            if (!StaticColor)
                ColorBuffer.SetData(ColorData, 0, 0, activeProjectileCount);
            args[1] = (uint)activeProjectileCount;
            ArgsBuffer.SetData(args);

            // Instruct the GPU to draw
            Graphics.DrawMeshInstancedIndirect(Mesh, 0, Material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), ArgsBuffer);
        }

        // Cleanup
        public void ReleaseBuffers(bool releaseArgs)
        {
            if (TransformBuffer != null)
                TransformBuffer.Release();
            TransformBuffer = null;

            if (ColorBuffer != null)
                ColorBuffer.Release();
            ColorBuffer = null;

            if (releaseArgs)
            {
                if (ArgsBuffer != null)
                    ArgsBuffer.Release();
                ArgsBuffer = null;
            }
        }
    }
}