using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    [RequireComponent(typeof(Collider))]
    public class WaterForceHandler : MonoBehaviour
    {
        [SerializeField] float _SurfaceHeight =10;
        RenderTexture _LastHeightMap;
        Texture2D _HeightMap;

        Collider _Collider;
        // Start is called before the first frame update
        void Start()
        {
            _Collider = GetComponent<Collider>();
        }

        // Update is called once per frame
        void Update()
        {
            _LastHeightMap = SceneData.Instance.WavePropagation.HeightMap;
            RenderHelpers.ToTexture2DNoAlloc(_LastHeightMap, ref _HeightMap);
        }

        public bool GetPosToSurfaceOffset(Vector3 position, out float offset)
        {
            offset = 0f;
            if (_Collider.bounds.Contains(position))
            {
                Vector2Int texPos = WorldPosToTexturePos(position, SceneData.Instance.SimData.TextureSize);
                Color c =_HeightMap.GetPixel(texPos.x, texPos.y);

                float heightMapValue = (c.b - 0.5f) * SceneData.Instance.SimData.HeightScalar;
                float height = transform.position.y + _SurfaceHeight + heightMapValue ;
                offset = height - position.y;
                return height > position.y;
            }

            return false;
        }

        Vector2Int WorldPosToTexturePos(Vector3 worldPos, int textureSize)
        {
            Vector3 temp = worldPos - _Collider.bounds.min;
            Vector2 normalised2DPos = new Vector2(temp.x / _Collider.bounds.size.x, temp.z / _Collider.bounds.size.z);
            return new Vector2Int(Mathf.RoundToInt(normalised2DPos.x * textureSize), Mathf.RoundToInt(normalised2DPos.y * textureSize));
        }
    }
}
