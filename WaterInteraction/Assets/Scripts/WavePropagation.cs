using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{

    public struct Coord
    {
        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X;
        public int Y;
    }

    public class WavePropagation : MonoBehaviour
    {
        // Start is called before the first frame update

        [SerializeField] ComputeShader _WavePropagationShader;
        [SerializeField] Material _TargetMaterial;
        RenderTexture _TargetTexture;
        const int _TextureSize = 32;

        //Wave Propagation
        int _WavePropagationHandle;

        //Adding new Waves
        int _AddNewWavesHandle;
        List<Coord> _NewWaveOrigins = new List<Coord>();
        ComputeBuffer _NewWaveOriginsBuffer;



        void InitializeWavePropagationShader()
        {
            _WavePropagationHandle = _WavePropagationShader.FindKernel("PropagateWaves");
            _AddNewWavesHandle = _WavePropagationShader.FindKernel("AddNewWaves");
        }
        void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "WavePropagationTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }
        void CreateBuffers()
        {
            int stride = sizeof(int) * 2;
            int amountOfStrides = 10;
            _NewWaveOriginsBuffer = new ComputeBuffer(stride * amountOfStrides, stride, ComputeBufferType.Append, ComputeBufferMode.Immutable);
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

            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            _NewWaveOrigins.Add(new Coord(Random.Range(0, _TextureSize), Random.Range(0, _TextureSize)));
            //RunWavePropagation();
        }
        private void OnDestroy()
        {
            if (_NewWaveOriginsBuffer.IsValid()) _NewWaveOriginsBuffer.Dispose();
        }

        void RunWavePropagation()
        {
            //Set Data
            _WavePropagationShader.SetTexture(_WavePropagationHandle, "TargetTexture", _TargetTexture);

            //Run Shader
            _WavePropagationShader.Dispatch(_WavePropagationHandle, 1024 / 8, 1024 / 8, 1);

            Debug.Log("Wave Propagation ran");
        }

        void RunAddNewWaves()
        {
            if (_NewWaveOrigins.Count > 0)
            {
                _NewWaveOriginsBuffer.SetData(_NewWaveOrigins.ToArray());
                _NewWaveOriginsBuffer.SetCounterValue((uint)_NewWaveOrigins.Count);

                //Set Data
                _WavePropagationShader.SetTexture(_AddNewWavesHandle, "TargetTexture", _TargetTexture);
                _WavePropagationShader.SetBuffer(_AddNewWavesHandle, "NewCoords", _NewWaveOriginsBuffer);

                //Run Shader
                _WavePropagationShader.Dispatch(_AddNewWavesHandle, Mathf.CeilToInt(_NewWaveOrigins.Count / 8f), 1, 1);

                Debug.Log("new waves added");
                _NewWaveOrigins.Clear();
            }

        }

        // Update is called once per frame
        void Update()
        {
            RunAddNewWaves();
        }

        public void SpawnWave(Vector2 normalisedTexturePosition)
        {
            Coord coord = new Coord
            {
                X = Mathf.FloorToInt(normalisedTexturePosition.x * _TextureSize),
                Y = Mathf.FloorToInt(normalisedTexturePosition.y * _TextureSize),
            };
            Debug.Log("Coord: " + coord.X + ", " + coord.Y);
            _NewWaveOrigins.Add(coord);
        }
    }
}
