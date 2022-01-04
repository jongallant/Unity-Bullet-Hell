using UnityEngine;

namespace BulletHell
{
    public class PoolFallback<T> : Pool<T> where T : ProjectileData, new()
    {
        /// <summary>
        /// `GameObject` to put fallback `GameObject`s (as children)
        /// </summary>
        private static GameObject manager;
        public static GameObject Manager
        {
            get
            {
                manager ??= new GameObject("ProjectileManagerFallback");
                return manager;
            }
        }

        private static Texture2D GetTexture2DFromTexture(Texture texture)
        {
            Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 32);
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = currentRT;
            return texture2D;
        }

        private static Sprite GetSpriteFromTexture2D(Texture2D texture)
        {
            var texture2d = GetTexture2DFromTexture(texture);
            return Sprite.Create(texture2d, new Rect(0, 0, texture2d.width, texture2d.height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// The fallback projectile prefab to display
        /// </summary>
        private ProjectilePrefab prefab;

        public PoolFallback(int capacity, ProjectilePrefab prefab) : base(capacity)
        {
            // Make sure the `GameObject` instantiated later inactive at first, and not interfere with the original prefab
            var active = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);
            this.prefab = GameObject.Instantiate(prefab, Manager.transform);
            prefab.gameObject.SetActive(active);

            InitSprite();
        }

        private void InitSprite()
        {
            var renderer = prefab.gameObject.AddComponent<SpriteRenderer>();
            if (renderer) // not attached to a `SpriteRenderer`
            {
                renderer.sprite = prefab.Texture
                    ? GetSpriteFromTexture2D(GetTexture2DFromTexture(prefab.Texture)) // use specified sprite
                    : Resources.Load<Sprite>("Textures/circlewhite"); // use default sprite
            }
            renderer.sortingOrder = Mathf.CeilToInt(prefab.ZIndez); // `ZIndez` is floating point so it won't be the same but still try to keep consistency
        }

        public override void Clear()
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Item.FallBackObject) GameObject.Destroy(Nodes[i].Item.FallBackObject);
            }
            base.Clear();
        }

        public override Node Get()
        {
            var node = base.Get();
            node.Item.FallBackObject = GameObject.Instantiate(prefab.gameObject, Manager.transform);
            return node;
        }

        public override void Return(int index)
        {
            var fo = Nodes[index].Item.FallBackObject;
            if (fo) GameObject.Destroy(fo);
            Nodes[index].Item.FallBackObject = null;
            base.Return(index);
        }
    }
}