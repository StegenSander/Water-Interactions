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

        #region SerializeFields
        [SerializeField] ComputeShader _NavierStokesShader;
        [SerializeField] Material _TargetMaterial;
        [SerializeField] Texture2D _CollisionTexture;

        [Header("UpdateValues")]
        [SerializeField] float _InitialSpeedScalar;
        [SerializeField] float _DensityDiffuseRate;
        [SerializeField] float _VelocityDiffuseRate;
        [SerializeField] float _SpeedUpdateScalar;

        [Header("DebugOptions")]
        [SerializeField] bool _DiffuseTextures = true;
        [SerializeField] bool _UpdateAlongVelocityField = true;
        [SerializeField] DebugDraw _DebugDraw;
        [SerializeField] FilterMode _TextureFilter;
        #endregion

        #region Kernels
        int _KernelFillTexture;

        int _KernelCalculateTotalVolume;
        int _KernelCalculateOldVolume;
        int _KernelCalculateNewVolume;
        int _KernelFixVolume;

        int _KernelRenderHeightMap;
        int _KernelHandleSpawnWaves;
        int _KernelHandleNewWaves;
        int _KernelDiffuseDensity;
        int _KernelDiffuseVelocity;
        int _KernelHandleCollision;
        int _KernelUpdateDensityAlongVelocityField;
        int _KernelUpdateVelocityAlongVelocityField;
        #endregion

        List<NewWaveData> _NewWaves = new List<NewWaveData>();
        ComputeBuffer _NewWaveBuffer;

        ComputeBuffer _TotalVolume;

        RenderTexture _DynamicCollisionOld;
        public RenderTexture DynamicCollisionOld
        {
            set { _DynamicCollisionOld = value; }
        }

        RenderTexture _DynamicCollisionNew;
        public RenderTexture DynamicCollisionNew
        {
            set { _DynamicCollisionNew = value; }
        }

        float _TargetVolume;
        float _NewVolume;

        #region RenderTextures
        RenderTexture _TargetTexture;
        public RenderTexture HeightMap
        {
            get { return _TargetTexture; }
        }

        RenderTexture _VelocityField1;
        RenderTexture _VelocityField2;

        RenderTexture _DensityField1;
        RenderTexture _DensityField2;
        #endregion

        //Double buffer system;
        bool _IsTexture1Input = true;

        #region MonoBehaviourFunctions
        // Start is called before the first frame update
        void Start()
        {
            InitialiseComputeShader();
            InitialiseBuffers();
            InitialiseRenderTextures();
            _TargetMaterial.SetTexture("_BaseMap", _TargetTexture);
            _TargetMaterial.SetTexture("_InputMap", _TargetTexture);
            _TargetMaterial.SetFloat("_WaveHeightMultiplier", SceneData.Instance.SimData.HeightScalar);
            Debug.Log("Start finished");


            _TotalVolume = new ComputeBuffer(1, sizeof(float),ComputeBufferType.Default);
            _TargetVolume = CalculateTotalVolume();
        }

        private void OnDestroy()
        {
            _NewWaveBuffer.Release();
            _TotalVolume.Dispose();
        } 

        void Update()
        {
            HandleNewClickWaves();

            HandleNewCollisionWaves();

            if (_DiffuseTextures) DiffuseTextures();


            if (_UpdateAlongVelocityField) UpdateAlongVelocityField();


            //BAD CODE CHANGE THIS
            //FindObjectOfType<ApplyHeightMapToMesh>().SetHeightMap(_TargetTexture);

            _NewVolume = CalculateTotalVolume();

            //Debug.Log("Old Volume: " + _TargetVolume + ", New Volume:" + _NewVolume + " val: " + (_TargetVolume - _NewVolume) / (_SimData.TextureSize * +_SimData.TextureSize));
            FixVolume();
            SwapBuffer();


            //HandleCollision();
            RenderHeightMap();

            CleanUpFrame();
        }
        #endregion
        #region Initialize
        void InitialiseComputeShader()
        {
            _KernelFillTexture = _NavierStokesShader.FindKernel("FillTexture");

            _KernelCalculateTotalVolume = _NavierStokesShader.FindKernel("CalculateTotalVolume");
            _KernelCalculateOldVolume =_NavierStokesShader.FindKernel("CalculateOldVolume"); ;
            _KernelCalculateNewVolume =_NavierStokesShader.FindKernel("CalculateNewVolume"); ;
            _KernelFixVolume = _NavierStokesShader.FindKernel("FixVolume");

            _KernelRenderHeightMap = _NavierStokesShader.FindKernel("RenderHeightMap");
            _KernelHandleSpawnWaves = _NavierStokesShader.FindKernel("HandleSpawnWaves");
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
        #endregion

        #region InitializeRenderTexture
        void InitialiseRenderTextures()
        {
            int textureSize = SceneData.Instance.SimData.TextureSize;
            CreateRenderTexture(ref _TargetTexture, textureSize, textureSize);

            //Init Velocity Texture1
            CreateVelocityTexture(ref _VelocityField1, textureSize, textureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _VelocityField1);
            _NavierStokesShader.SetVector("TextureFillValue", Vector3.zero);
            _NavierStokesShader.Dispatch(_KernelFillTexture, textureSize / 8, textureSize / 8, 1);

            //Init Velocity Texture2
            CreateVelocityTexture(ref _VelocityField2, textureSize, textureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _VelocityField2);
            _NavierStokesShader.SetVector("TextureFillValue", Vector3.zero);
            _NavierStokesShader.Dispatch(_KernelFillTexture, textureSize / 8, textureSize / 8, 1);

            //Init Density Texture1
            CreateDensityTexture(ref _DensityField1, textureSize, textureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _DensityField1);
            _NavierStokesShader.SetVector("TextureFillValue", new Vector4(SceneData.Instance.SimData.DefaultDensityValue,0,0,1));
            _NavierStokesShader.Dispatch(_KernelFillTexture, textureSize / 8, textureSize / 8, 1);

            //Init Density Texture2
            CreateDensityTexture(ref _DensityField2, textureSize, textureSize);
            _NavierStokesShader.SetTexture(_KernelFillTexture, "TextureToFill", _DensityField2);
            _NavierStokesShader.SetVector("TextureFillValue", new Vector4(SceneData.Instance.SimData.DefaultDensityValue, 0, 0, 1));
            _NavierStokesShader.Dispatch(_KernelFillTexture, textureSize / 8, textureSize / 8, 1);
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
            _NavierStokesShader.Dispatch(_KernelRenderHeightMap, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
        }

        #region Volume
        float CalculateTotalVolume(bool reverse = false)
        {
            bool isBuffer1 = _IsTexture1Input;
            if (reverse)
            {
                isBuffer1 = !isBuffer1;
            }
            if (isBuffer1)
            {
                _NavierStokesShader.SetTexture(_KernelCalculateTotalVolume, "DensityFieldIN", _DensityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelCalculateTotalVolume, "DensityFieldIN", _DensityField2);
            }
            _NavierStokesShader.SetBuffer(_KernelCalculateTotalVolume, "TotalVolume", _TotalVolume);
            _NavierStokesShader.Dispatch(_KernelCalculateTotalVolume, 1, 1, 1);

            float[] array = new float[1];
            _TotalVolume.GetData(array);
            return array[0];
        }

        void CalculateOldVolume()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelCalculateOldVolume, "DensityFieldIN", _DensityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelCalculateOldVolume, "DensityFieldIN", _DensityField2);
            }

            _NavierStokesShader.Dispatch(_KernelCalculateOldVolume, 1, 1, 1);
        }
        void CalculateNewVolume()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelCalculateNewVolume, "DensityFieldIN", _DensityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelCalculateNewVolume, "DensityFieldIN", _DensityField2);
            }

            _NavierStokesShader.Dispatch(_KernelCalculateNewVolume, 1, 1, 1);
        }
        void FixVolume()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelFixVolume, "DensityFieldIN", _DensityField1);
                _NavierStokesShader.SetTexture(_KernelFixVolume, "DensityFieldOUT", _DensityField2);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelFixVolume, "DensityFieldIN", _DensityField2);
                _NavierStokesShader.SetTexture(_KernelFixVolume, "DensityFieldOUT", _DensityField1);
            }

            _NavierStokesShader.SetFloat( "OldVolume", _TargetVolume);
            _NavierStokesShader.SetFloat( "NewVolume", _NewVolume);
            _NavierStokesShader.Dispatch(_KernelFixVolume, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
        }
        #endregion

        #region NewWaves
        void HandleNewClickWaves()
        {
            if (_NewWaves.Count < 1) return;
            while (_NewWaves.Count % 8 != 0)
            {
                _NewWaves.Add(new NewWaveData() { normalisedPosition = new Vector2(-1, -1) });
            }
            _NavierStokesShader.SetBuffer(_KernelHandleNewWaves, "NewOccupiedWaveBuffer", _NewWaveBuffer);

            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "VelocityFieldOUT", _VelocityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelHandleNewWaves, "VelocityFieldOUT", _VelocityField2);
            }
            _NewWaveBuffer.SetData(_NewWaves, 0, 0, _NewWaves.Count);
            _NavierStokesShader.SetInt("TextureSize", SceneData.Instance.SimData.TextureSize);
            _NavierStokesShader.SetFloat("InitialSpeedScalar", _InitialSpeedScalar);
            _NavierStokesShader.Dispatch(_KernelHandleNewWaves, Mathf.CeilToInt(_NewWaves.Count), 1, 1);
            //Debug.Log("Waves added");
        }
        void HandleNewCollisionWaves()
        {
            if (_IsTexture1Input)
            {
                _NavierStokesShader.SetTexture(_KernelHandleSpawnWaves, "VelocityFieldOUT", _VelocityField1);
            }
            else
            {
                _NavierStokesShader.SetTexture(_KernelHandleSpawnWaves, "VelocityFieldOUT", _VelocityField2);
            }

            _NavierStokesShader.SetTexture(_KernelHandleSpawnWaves, "DynamicCollisionOld", _DynamicCollisionOld);
            _NavierStokesShader.SetTexture(_KernelHandleSpawnWaves, "DynamicCollisionNew", _DynamicCollisionNew);
            _NavierStokesShader.SetInt("TextureSize", SceneData.Instance.SimData.TextureSize);
            _NavierStokesShader.SetFloat("InitialSpeedScalar", _InitialSpeedScalar);
            _NavierStokesShader.Dispatch(_KernelHandleSpawnWaves, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
            //Debug.Log("Waves added");
        }
        #endregion

        #region DiffuseWaves
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
            _NavierStokesShader.Dispatch(_KernelDiffuseDensity, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
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
            _NavierStokesShader.Dispatch(_KernelDiffuseVelocity, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
        }
        #endregion

        #region UpdateAlongVelocityField
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

            _NavierStokesShader.SetFloat("SpeedScalar", _SpeedUpdateScalar);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelUpdateDensityAlongVelocityField, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
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

            _NavierStokesShader.SetFloat("SpeedScalar", _SpeedUpdateScalar);
            _NavierStokesShader.SetFloat("DeltaTime", Time.deltaTime);
            _NavierStokesShader.Dispatch(_KernelUpdateVelocityAlongVelocityField, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
        }
        #endregion

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
            _NavierStokesShader.Dispatch(_KernelHandleCollision, SceneData.Instance.SimData.TextureSize / 8, SceneData.Instance.SimData.TextureSize / 8, 1);
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
                    _NewWaves.Add(new NewWaveData() { normalisedPosition = normalisedPosition 
                        + new Vector2(x / SceneData.Instance.SimData.TextureSize, y / SceneData.Instance.SimData.TextureSize), densityChange = 2f });
                }
            }
        }
    }
}
