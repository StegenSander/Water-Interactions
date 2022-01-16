using System.Collections.Generic;
using UnityEngine;
using System;

namespace WaterInteraction
{
    [Serializable]
    struct WaveCellGPU
    {
        public float Strenght;
        public float Speed;
        public float DecayPerCellMoved;
        public Vector2Int Origin;
    }

    public class WavePropagationGPU : MonoBehaviour
    {
        // Start is called before the first frame update

        [SerializeField] ComputeShader _WavePropagationShader;
        [SerializeField] Material _TargetMaterial;
        RenderTexture _TargetTexture;
        const int _TextureSize = 32;

        //Wave Propagation
        int _WavePropagationHandle;

        //Double buffer concept
        ComputeBuffer _WaveCellsBuffer1;
        ComputeBuffer _WaveCellsBuffer2;
        bool _UseWaveCellsBuffer1AsInput;

        //Adding new Waves
        int _AddNewWavesHandle;
        List<WaveCellGPU> _NewWaveOrigins = new List<WaveCellGPU>();
        ComputeBuffer _NewWaveOriginsBuffer;

        //CalculateFinalWaveMap
        int _CalculateFinalWaveMapHandle;

        void InitializeWavePropagationShader()
        {
            _WavePropagationHandle = _WavePropagationShader.FindKernel("PropagateWaves");
            _AddNewWavesHandle = _WavePropagationShader.FindKernel("AddNewWaves");
            _CalculateFinalWaveMapHandle = _WavePropagationShader.FindKernel("CalculateFinalWaveMap");
        }
        void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "WavePropagationTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }
        void CreateBuffers()
        {
            //New Waves buffer
            {
                int stride = sizeof(float) * 3 + sizeof(int) * 2;
                int count = 10;
                _NewWaveOriginsBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Append, ComputeBufferMode.Immutable);
            }

            //WaveCellBuffers
            {
                int stride = sizeof(float) * 3 + sizeof(int) * 2;
                int count = _TextureSize * _TextureSize;

                _WaveCellsBuffer1 = new ComputeBuffer(count, stride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                _WaveCellsBuffer2 = new ComputeBuffer(count, stride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            }
        }

        void InitializeRenderTarget()
        {
            CreateRenderTexture(ref _TargetTexture, _TextureSize, _TextureSize);
            _TargetMaterial.SetTexture("_BaseMap", _TargetTexture);
        }
        void Start()
        {
            InitializeWavePropagationShader();
            InitializeRenderTarget();
            CreateBuffers();

            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
            SpawnWave(new Vector2(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f)));
        }
        private void OnDestroy()
        {
            if (_NewWaveOriginsBuffer.IsValid()) _NewWaveOriginsBuffer.Dispose();
            if (_WaveCellsBuffer1.IsValid()) _WaveCellsBuffer1.Dispose();
            if (_WaveCellsBuffer2.IsValid()) _WaveCellsBuffer2.Dispose();
        }


        void RunAddNewWaves()
        {
            if (_NewWaveOrigins.Count > 0)
            {
                while (_NewWaveOrigins.Count % 8 != 0)
                {
                    _NewWaveOrigins.Add(new WaveCellGPU() { Origin = new Vector2Int(-1,-1)});
                }

                _NewWaveOriginsBuffer.SetData(_NewWaveOrigins.ToArray());
                _NewWaveOriginsBuffer.SetCounterValue((uint)_NewWaveOrigins.Count);

                _WavePropagationShader.SetInt("TextureWidth", _TextureSize);
                _WavePropagationShader.SetInt("TextureHeight", _TextureSize);

                //Set Data
                if (_UseWaveCellsBuffer1AsInput)
                {
                    _WavePropagationShader.SetBuffer(_AddNewWavesHandle, "TargetWaveCellBuffer", _WaveCellsBuffer1);
                }
                else
                {
                    _WavePropagationShader.SetBuffer(_AddNewWavesHandle, "TargetWaveCellBuffer", _WaveCellsBuffer2);
                }

                _WavePropagationShader.SetBuffer(_AddNewWavesHandle, "NewCoords", _NewWaveOriginsBuffer);

                //Run Shader
                _WavePropagationShader.Dispatch(_AddNewWavesHandle, Mathf.CeilToInt(_NewWaveOrigins.Count / 8f), 1, 1);

                Debug.Log("new waves added");
                _NewWaveOrigins.Clear();
            }

        }

        void RunWavePropagation()
        {
            //Set Data
            _WavePropagationShader.SetFloat("DeltaTime", Time.deltaTime);
            _WavePropagationShader.SetFloat("DecayPerSecond", 0.1f);

            _WavePropagationShader.SetInt("TextureWidth", _TextureSize);
            _WavePropagationShader.SetInt("TextureHeight", _TextureSize);

            if (_UseWaveCellsBuffer1AsInput)
            {
                _WavePropagationShader.SetBuffer(_WavePropagationHandle, "ReadWaveCellBuffer", _WaveCellsBuffer1);
                _WavePropagationShader.SetBuffer(_WavePropagationHandle, "TargetWaveCellBuffer", _WaveCellsBuffer2);
            }
            else
            {
                _WavePropagationShader.SetBuffer(_WavePropagationHandle, "ReadWaveCellBuffer", _WaveCellsBuffer2);
                _WavePropagationShader.SetBuffer(_WavePropagationHandle, "TargetWaveCellBuffer", _WaveCellsBuffer1);
            }

            //Run Shader
            _WavePropagationShader.Dispatch(_WavePropagationHandle, Mathf.CeilToInt(_TextureSize / 8f), Mathf.CeilToInt(_TextureSize / 8f), 1);

            Debug.Log("Wave Propagation ran");
        }

        void RunCalculateFinalHeightMap()
        {
            _WavePropagationShader.SetInt("TextureWidth", _TextureSize);
            _WavePropagationShader.SetInt("TextureHeight", _TextureSize);

            if (_UseWaveCellsBuffer1AsInput)
            {
                _WavePropagationShader.SetBuffer(_CalculateFinalWaveMapHandle, "ReadWaveCellBuffer", _WaveCellsBuffer1);
            }
            else
            {
                _WavePropagationShader.SetBuffer(_CalculateFinalWaveMapHandle, "ReadWaveCellBuffer", _WaveCellsBuffer2);
            }

            _WavePropagationShader.SetTexture(_CalculateFinalWaveMapHandle, "TargetTexture", _TargetTexture);
            _WavePropagationShader.Dispatch(_CalculateFinalWaveMapHandle, Mathf.CeilToInt(_TextureSize / 8f), Mathf.CeilToInt(_TextureSize / 8f), 1);
            //Debug.Log("final height map calculated");
        }

        // Update is called once per frame
        void Update()
        {
            RunAddNewWaves();
            RunWavePropagation();
            RunCalculateFinalHeightMap();

            SwapBuffer();
        }

        void SwapBuffer()
        {
            _UseWaveCellsBuffer1AsInput = !_UseWaveCellsBuffer1AsInput;
        }

        public void SpawnWave(Vector2 normalisedTexturePosition)
        {
            WaveCellGPU cell = new WaveCellGPU
            {
                Origin = new Vector2Int(Mathf.FloorToInt(normalisedTexturePosition.x * _TextureSize)
                    , Mathf.FloorToInt(normalisedTexturePosition.y * _TextureSize)),
                Strenght = 1f,

            };
            Debug.Log("Coord: " + cell.Origin.x + ", " + cell.Origin.y);
            _NewWaveOrigins.Add(cell);
        }
    }
}
