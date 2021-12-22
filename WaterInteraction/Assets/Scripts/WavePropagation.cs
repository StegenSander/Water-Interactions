using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
namespace WaterInteraction
{
    public struct WaveSegment
    {
        public Vector2 Origin;

        public float Strength;
        public float StrenghtDecay;

        public float Radius;
        public float Speed;

        public float WaveThickness;

        public float StartAngleRadian;
        public float AngleSize;
    }

    public class WavePropagation : MonoBehaviour
    {
        List<WaveSegment> _Waves = new List<WaveSegment>();

        [SerializeField] Material _TargetMaterial;

        [Header("Wave Data")]
        [SerializeField] float _DefaultWaveStrenght = 1f;
        [SerializeField] float _DefaultDecayPerSecond = 1f;
        [SerializeField] float _DefaultSpeed = 2f;
        [SerializeField] float _DefaultLineThickness = 0.1f;

        RenderTexture _TargetTexture;
        const int _TextureSize = 1024;
        
        void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "WavePropagationTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }

        //void ClearTargetTexture()
        //{
        //    for (int x = 0; x < _TextureSize; x++)
        //    {
        //        for (int y = 0; y < _TextureSize; y++)
        //        {
        //            _TargetTexture.SetPixel(x, y, Color.black);
        //        }
        //    }
        //}

        // Start is called before the first frame update
        void Start()
        {
            CreateRenderTexture(ref _TargetTexture, _TextureSize, _TextureSize);
            //ClearTargetTexture();
            //ApplyTexture();
            _TargetMaterial.SetTexture("_BaseMap", _TargetTexture);
        }

        // Update is called once per frame
        void Update()
        {
            //ClearTargetTexture();
            UpdateWaves();
            ValidateWaves();
            FindObjectOfType<WaveDrawer>().DrawAllWaveSegments(_TargetTexture, _Waves);
            //DrawWaves();
            //ApplyTexture();
        }
        void UpdateWaves()
        {
            Profiler.BeginSample("UpdatingWaves");
            for (int i =0; i < _Waves.Count; i++)
            {
                WaveSegment w = _Waves[i];
                w.Strength -= w.StrenghtDecay * Time.deltaTime;
                w.Radius += w.Speed * Time.deltaTime;
                _Waves[i] = w;
            }
            Profiler.EndSample();
        }
        void ValidateWaves()
        {
            List<WaveSegment> _WavesToDestroy = new List<WaveSegment>();
            foreach (WaveSegment wave in _Waves)
            {
                if (wave.Strength < 0f) _WavesToDestroy.Add(wave);
                else if(wave.Radius > 1f) _WavesToDestroy.Add(wave);
            }
            foreach (WaveSegment wave in _WavesToDestroy)
            {
                _Waves.Remove(wave);
            }
        }
        //void DrawWaves()
        //{
        //    foreach(WaveSegment wave in _Waves)
        //    {
        //        DrawCircle(_TargetTexture
        //            , Mathf.RoundToInt(wave.Origin.x * _TextureSize)
        //            , Mathf.RoundToInt(wave.Origin.y * _TextureSize)
        //            , Mathf.RoundToInt(wave.Radius * _TextureSize)
        //            , Mathf.RoundToInt(wave.Radius * _TextureSize - 3)
        //            , wave.StartAngleRadian
        //            , wave.AngleSize
        //            , new Color(wave.Strength, 0, 0));
        //    }
        //}
        //void ApplyTexture()
        //{
        //    _TargetTexture.Apply();
        //}

        public void SpawnWave(Vector2 normalisedPosition)
        {
            _Waves.Add(new WaveSegment()
            {
                Origin = normalisedPosition,
                Radius = 0f,
                Speed = _DefaultSpeed,
                StrenghtDecay = _DefaultDecayPerSecond,
                Strength = _DefaultWaveStrenght,
                StartAngleRadian = 0,
                AngleSize = 6.28f,
                WaveThickness = _DefaultLineThickness,
            }) ;


            //StartAngleRadian = Random.Range(0, 6.28f),
            //    AngleSize = Random.Range(0, 6.28f),
        }

        //public void DrawCircle(Texture2D tex, int circleX, int circleY, int outerRadius, int innerRadius, float startAngle, float angleSize, Color col)
        //{
        //    Profiler.BeginSample("DrawingCirlce");
        //    int minXBound = Mathf.Max(circleX - outerRadius,0);
        //    int maxXBound = Mathf.Min(circleX + outerRadius,tex.width);

        //    int minYBound = Mathf.Max(circleY - outerRadius, 0);
        //    int maxYBound = Mathf.Min(circleY + outerRadius, tex.width);

        //    float sqrOuterRadius = outerRadius * outerRadius;
        //    float sqrInnerRadius = innerRadius * innerRadius;

        //    for (int x = minXBound; x < maxXBound; x++)
        //    {
        //        for (int y = minYBound; y < maxYBound; y++)
        //        {
        //            Vector2 posDiff = new Vector2(x, y) - new Vector2(circleX, circleY);
        //            float angle = Mathf.Atan2(posDiff.y, posDiff.x) + 3.14f;
        //            float sqrDistanceToOrigin = posDiff.sqrMagnitude;

        //            if (sqrDistanceToOrigin < sqrOuterRadius 
        //                && sqrDistanceToOrigin > sqrInnerRadius 
        //                && angle > startAngle 
        //                && angle < startAngle + angleSize )
        //            {
        //                tex.SetPixel(x, y, col);
        //            }
        //        }
        //    }
        //    Profiler.EndSample();
        //}
    }
}
