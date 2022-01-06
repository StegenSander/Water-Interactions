using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WaterInteraction
{
    [RequireComponent(typeof(Camera))]
    public class CollisionRender : MonoBehaviour
    {
        [SerializeField] SimulationData _SimData;
        [SerializeField] Material _DebugMat1;
        [SerializeField] Material _DebugMat2;

        NavierStokesPropagation _WavePropagation;

        Camera _CollisionCamera;
        RenderTexture _CollisionTexture1;
        RenderTexture _CollisionTexture2;
        bool _Is1NewCollisionTexture = true;

        public RenderTexture NewCollisionTexture
        {
            get {
                if (_Is1NewCollisionTexture) return _CollisionTexture1;
                else return _CollisionTexture2;
            }
        }

        public RenderTexture OldCollisionTexture
        {
            get
            {
                if (!_Is1NewCollisionTexture) return _CollisionTexture1;
                else return _CollisionTexture2;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            _CollisionCamera = GetComponent<Camera>();
            CreateRenderTexture(ref _CollisionTexture1, _SimData.TextureSize);
            CreateRenderTexture(ref _CollisionTexture2, _SimData.TextureSize);

            _WavePropagation = FindObjectOfType<NavierStokesPropagation>();
            _WavePropagation.DynamicCollisionNew = NewCollisionTexture;
            _WavePropagation.DynamicCollisionOld = OldCollisionTexture;


            _DebugMat1.SetTexture("_BaseMap", _CollisionTexture1);
            _DebugMat2.SetTexture("_BaseMap", _CollisionTexture2);
        }

        // Update is called once per frame
        void Update()
        {
            if (_Is1NewCollisionTexture)
            {
                RenderCollision(_CollisionTexture1);
            }
            else
            {
                RenderCollision(_CollisionTexture2);
            }

            _Is1NewCollisionTexture = !_Is1NewCollisionTexture;
        }

        void RenderCollision(RenderTexture targetTexture)
        {
            _CollisionCamera.forceIntoRenderTexture = true;
            _CollisionCamera.targetTexture = targetTexture;

            _WavePropagation.DynamicCollisionNew = NewCollisionTexture;
            _WavePropagation.DynamicCollisionOld = OldCollisionTexture;
        }

        void CreateRenderTexture(ref RenderTexture texture, int size)
        {
            texture = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "CollisionTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.enableRandomWrite = true;
            texture.Create();
        }
    }
}