using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{

    public class NavierStokesPropagation : MonoBehaviour
    {
        public enum DebugDraw
        {
            DrawDensity,
            DrawVelocity,
            DrawBoth,
        }
        struct NewWaveData
        {
            public Vector2 normalisedPosition;
            public float densityChange;
        }

        [SerializeField] ComputeShader _NavierStokesShader;
        [SerializeField] Material _TargetMaterial;
        [SerializeField] Texture2D _CollisionTexture;
        [SerializeField] int _TextureSize =32;

        [Header("DefaultValues")]
        [SerializeField] Vector4 _VelocityFieldStartValues;
        [SerializeField] Vector4 _DensityFieldStartValues;

        [Header("UpdateValues")]
        [SerializeField] float _DensityDiffuseRate;
        [SerializeField] float _VelocityDiffuseRate;
        [SerializeField] float _SpeedScalar;

        [Header("DebugOptions")]
        [SerializeField] bool _DiffuseTextures = true;
        [SerializeField] bool _UpdateAlongVelocityField = true;
        [SerializeField] DebugDraw _DebugDraw;
        [SerializeField] FilterMode _TextureFilter;

        int _KernelFillTexture;
        int _KernelRenderHeightMap;
        int _KernelHandleNewWaves;
        int _KernelDiffuseDensity;
        int _KernelDiffuseVelocity;
        int _KernelHandleCollision;
        int _KernelUpdateDensityAlongVelocityField;
        int _KernelUpdateVelocityAlongVelocityField;

        List<NewWaveData> _NewWaves = new List<NewWaveData>();
        ComputeBuffer _NewWaveBuffer;

        #region RenderTextures
        RenderTexture _TargetTexture;

        RenderTexture _VelocityField1;
        RenderTexture _VelocityField2;

        RenderTexture _DensityField1;
        RenderTexture _DensityField2;
        #endregion

        //Double buffer system;
        bool _IsTexture1Input = true;

        // Start is called before the first frame update
        void Start()
        {
            InitialiseComputeShader();
            InitialiseBuffers();
            InitialiseRenderTextures();
            _TargetMaterial.SetTexture("_BaseMap", _TargetTexture);
            _TargetMaterial.SetTexture("_InputMap", _TargetTexture);
            Debug.Log("Start finished");
        }

        private void OnDestroy()
        {
            _NewWaveBuffer.Release();
        }

        void InitialiseComputeShader()
        {
            _KernelFillTexture = _NavierStokesShader.FindKernel("FillTexture");
            _KernelRenderHeightMap = _NavierStokesShader.FindKernel("RenderHeightMap");
            _KernelHandleNewWaves = _NavierStokesShader.FindKernel("HandleNewWaves");
            _KernelDiffuseDensity = _NavierStokesShader.FindKernel("DiffuseDensity");
            _KernelDiffuseVelocity = _NavierStokesShader.FindKernel("DiffuseVelocity");
            _KernelHandleCollision = _NavierStokesShader.FindKernel("HandleCollision");
            _KernelUpdateDensityAlongVelocityField = _NavierStokesShader.FindKernel("UpdateDensityAlongVelocityField");
            _KernelUpdateVelocityAlongVelocityField = _NavierStokesShader.FindKernel("UpdateVelocityAlongVelocityField");
        }

        void InitialiseBuffers()
        {
            int stride = sizeof(float) * 3; //SizeOfWaveSegment
            int count = 1024;
            _NewWaveBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        }

        #region InitializeRenderTexture
        void InitialiseRenderTextures()
        {
            CreateRenderTexture(ref _TargetTexture, _TextureSize, _TextureSize);

            //Init Velocity Texture1
            CreateVelocityTexture(ref _VelocityField1, _TextureSize, _TextureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _VelocityField1);
            _NavierStokesShader.SetVector("TextureFillValue", _VelocityFieldStartValues);
            _NavierStokesShader.Dispatch(_KernelFillTexture, _TextureSize / 8, _TextureSize / 8, 1);

            //Init Velocity Texture2
            CreateVelocityTexture(ref _VelocityField2, _TextureSize, _TextureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _VelocityField2);
            _NavierStokesShader.SetVector("TextureFillValue", _VelocityFieldStartValues);
            _NavierStokesShader.Dispatch(_KernelFillTexture, _TextureSize / 8, _TextureSize / 8, 1);

            //Init Density Texture1
            CreateDensityTexture(ref _DensityField1, _TextureSize, _TextureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _DensityField1);
            _NavierStokesShader.SetVector("TextureFillValue", _DensityFieldStartValues);
            _NavierStokesShader.Dispatch(_KernelFillTexture, _TextureSize / 8, _TextureSize / 8, 1);

            //Init Density Texture2
            CreateDensityTexture(ref _DensityField2, _TextureSize, _TextureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _DensityField2);
            _NavierStokesShader.SetVector("TextureFillValue", _DensityFieldStartValues);
            _NavierStokesShader.Dispatch(_KernelFillTexture, _TextureSize / 8, _TextureSize / 8, 1);
        }
        void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = _TextureFilter;
            texture.name = "TargetTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }

        void CreateVelocityTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "VelocityTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.enableRandomWrite = true;
            texture.Create();
        }
        void CreateDensityTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "DensityTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.enableRandomWrite = true;
            texture.Create();
        }
        #endregion

        // Update is called once per frame
        void Update()
        {
            HandleNewWaves();
            if (_DiffuseTextures) DiffuseTextures();
            if (_UpdateAlongVelocityField) UpdateAlongVelocityField();
            //HandleCollision();
            RenderHeightMap();

            CleanUpFrame();
        }

        #region UpdateMethods
        void RenderHeightMap()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "DensityFieldIN", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "VelocityFieldIN", _VelocityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "DensityFieldIN", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "VelocityFieldIN", _VelocityField2);
            }
            _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "CollisionTexture", _CollisionTexture);
            _NavierStokesShader.SetTexture(_KernelRenderHeightMap, "TargetTexture", _TargetTexture);
            _NavierStokesShader.SetInt("DebugDraw", (int)_DebugDraw);
            _NavierStokesShader.Dispatch(_KernelRenderHeightMap, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void HandleNewWaves()
        {
            if (_NewWaves.Count < 1) return;
            while (_NewWaves.Count % 8 != 0)
            {
                _NewWaves.Add(new NewWaveData() { normalisedPosition = new Vector2(-1, -1) });
            }
            _NavierStokesShader.SetBuffer(_KernelHandleNewWaves, "NewOccupiedWaveBuffer", _NewWaveBuffer);

            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "DensityFieldOUT", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "VelocityFieldOUT", _VelocityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "DensityFieldOUT", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "VelocityFieldOUT", _VelocityField2);
            }
            _NewWaveBuffer.SetData(_NewWaves, 0, 0, _NewWaves.Count);
            _NavierStokesShader.SetInt("TextureSize", _TextureSize);
            _NavierStokesShader.Dispatch(_KernelHandleNewWaves, Mathf.CeilToInt(_NewWaves.Count), 1, 1);
            Debug.Log("Waves added");
        }
        void DiffuseTextures()
        {
            DiffuseDensity();
            DiffuseVelocity();
            SwapBuffer();
        }

        void DiffuseDensity()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelDiffuseDensity, "DensityFieldIN", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelDiffuseDensity, "DensityFieldOUT", _DensityField2);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelDiffuseDensity, "DensityFieldIN", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelDiffuseDensity, "DensityFieldOUT", _DensityField1);
            }


            _NavierStokesShader.SetFloat("DiffuseRate", _DensityDiffuseRate);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelDiffuseDensity, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void DiffuseVelocity()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelDiffuseVelocity, "VelocityFieldIN", _VelocityField1);
                _NavierStokesShader.SetTexture(_KernelDiffuseVelocity, "VelocityFieldOUT", _VelocityField2);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelDiffuseVelocity, "VelocityFieldIN", _VelocityField2);
                _NavierStokesShader.SetTexture(_KernelDiffuseVelocity, "VelocityFieldOUT", _VelocityField1);
            }


            _NavierStokesShader.SetFloat("DiffuseRate", _VelocityDiffuseRate);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelDiffuseVelocity, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void UpdateAlongVelocityField()
        {
            UpdateDensityAlongVelocityField();
            UpdateVelocityAlongVelocityField();
            SwapBuffer();
        }

        void UpdateDensityAlongVelocityField()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "DensityFieldIN", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "DensityFieldOUT", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "VelocityFieldIN", _VelocityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "DensityFieldIN", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "DensityFieldOUT", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelUpdateDensityAlongVelocityField, "VelocityFieldIN", _VelocityField2);
            }

            _NavierStokesShader.SetFloat("SpeedScalar", _SpeedScalar);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelUpdateDensityAlongVelocityField, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void UpdateVelocityAlongVelocityField()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelUpdateVelocityAlongVelocityField, "VelocityFieldIN", _VelocityField1);
                _NavierStokesShader.SetTexture(_KernelUpdateVelocityAlongVelocityField, "VelocityFieldOUT", _VelocityField2);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelUpdateVelocityAlongVelocityField, "VelocityFieldIN", _VelocityField2);
                _NavierStokesShader.SetTexture(_KernelUpdateVelocityAlongVelocityField, "VelocityFieldOUT", _VelocityField1);
            }

            _NavierStokesShader.SetFloat("SpeedScalar", _SpeedScalar);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelUpdateVelocityAlongVelocityField, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void HandleCollision()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelHandleCollision, "DensityFieldOUT", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelHandleCollision, "VelocityFieldOUT", _VelocityField1);
            }                                  
            else                               
            {                                  
                _NavierStokesShader.SetTexture(_KernelHandleCollision, "DensityFieldOUT", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelHandleCollision, "VelocityFieldOUT", _VelocityField2);
            }
            _NavierStokesShader.SetTexture(_KernelHandleCollision, "CollisionTexture", _CollisionTexture);
            _NavierStokesShader.Dispatch(_KernelHandleCollision, _TextureSize / 8, _TextureSize / 8, 1);
        }

        void SwapBuffer()
        {
            _IsTexture1Input = !_IsTexture1Input;
        }

        void CleanUpFrame()
        {
            _NewWaves.Clear();
        }
        #endregion

        public void SpawnWave(Vector2 normalisedPosition)
        {
            const int range = 5;


            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    _NewWaves.Add(new NewWaveData() { normalisedPosition = normalisedPosition + new Vector2(x / _TextureSize, y / _TextureSize), densityChange = 2f });
                }
            }
        }
    }
}
