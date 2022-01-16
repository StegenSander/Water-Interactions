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

        public int IsSegmented;
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

        [Header("Collision Detection")]
        [SerializeField] Texture2D _CollisionTexture;
        [SerializeField] int _CircleIterationAtRadius1 = 30;

        [Header("Wave Behaviour")]
        [SerializeField] bool _EnableBounceBack = false;

        RenderTexture _TargetTexture;
        const int _TextureSize = 1024;

        List<WaveSegment> _WavesToDestroy = new List<WaveSegment>();
        WaveSegmentObjectManager _WaveManager;

        void CreateRenderTexture(ref RenderTexture texture, int width, int height)
        {
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "WavePropagationTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }

        // Start is called before the first frame update
        void Start()
        {
            CreateRenderTexture(ref _TargetTexture, _TextureSize, _TextureSize);
            //ClearTargetTexture();
            //ApplyTexture();
            _TargetMaterial.SetTexture("_BaseMap", _TargetTexture);

            _WaveManager = FindObjectOfType<WaveSegmentObjectManager>();

            Profiler.enableAllocationCallstacks = false;


            //Debug Collision Texture
            Texture2D temp = _CollisionTexture;
            _CollisionTexture = new Texture2D(_CollisionTexture.width, _CollisionTexture.height, TextureFormat.RGBAFloat, false);
            _CollisionTexture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < _CollisionTexture.width; x ++)
            {
                for (int y = 0; y < _CollisionTexture.width; y++)
                {
                    _CollisionTexture.SetPixel(x, y, temp.GetPixel(x, y));
                }
            }
            _CollisionTexture.Apply();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateWaves();
            ValidateWaves();
            UpdateAllWaveSegmentAngles();
            FindObjectOfType<WaveDrawer>().DrawAllWaveSegments(_TargetTexture, _CollisionTexture, _Waves);
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
            Profiler.BeginSample("Validate Waves");
            foreach (WaveSegment wave in _Waves)
            {
                if (!IsValidSegment(wave))
                {
                    _WavesToDestroy.Add(wave);
                    //Debug.Log("Wave Destroyed: Strength below 0");
                }
            }
            foreach (WaveSegment wave in _WavesToDestroy)
            {
                //_Waves.Remove(wave);
                _Waves.RemoveAll(waveSegment => _WavesToDestroy.Contains(waveSegment));
                _WaveManager.ReturnWave(wave);
            }
            _WavesToDestroy.Clear();

            Profiler.EndSample();
        }

        bool IsValidSegment(WaveSegment wave)
        {
            return !(wave.Strength < 0f || wave.Radius > 1f || wave.AngleSize < 0.1f);
        }

        void UpdateAllWaveSegmentAngles()
        {
            //Debug.Log("----NEW FULL LOOP----");
            int count = _Waves.Count;
            //We loop with an outdate count value, so we can add segments during the for loop
            for (int i = 0; i < count; i++)
            {
                _Waves[i] = UpdateWaveSegmentAngle(_Waves[i]);
            }
            _CollisionTexture.Apply();

        }
        WaveSegment UpdateWaveSegmentAngle(WaveSegment wave)
        {
            float iterationAngle = 6.28f / (_CircleIterationAtRadius1 * wave.Radius);
            //Debug.Log("iterationAngle: " + iterationAngle);


            Vector2 pos = new Vector2();
            Vector2 loopingPos = new Vector2();
            for (float currentAngle = wave.StartAngleRadian;
                currentAngle < wave.StartAngleRadian + wave.AngleSize;
                currentAngle += iterationAngle)
            {
                Profiler.BeginSample("Collision Check");
                //POS FUNCTION WRONG
                pos.x = wave.Origin.x + Mathf.Cos(currentAngle) * wave.Radius;
                pos.y= wave.Origin.y + Mathf.Sin (currentAngle) * wave.Radius;
                //pos = wave.Origin + 
                //    new Vector2(Mathf.Cos(currentAngle) * wave.Radius
                //    , Mathf.Sin(currentAngle) * wave.Radius);
                //Debug.Log("CurrentAngleRAD: " + currentAngle + "currentAngleDeg: " + currentAngle + "cos: " + Mathf.Cos(currentAngle) + "sin: " + Mathf.Sin(currentAngle));

                bool isCurrentAngleCollision = IsCollisionAtLocation(pos);

                Profiler.EndSample();

                //Debug CollisionHits
                //if (isCurrentAngleCollision)
                //{
                //    int x = Mathf.RoundToInt(pos.x * _CollisionTexture.width);
                //    int y = Mathf.RoundToInt(pos.y * _CollisionTexture.height);
                //    _CollisionTexture.SetPixel(x, y, Color.blue);
                //}

                if (isCurrentAngleCollision)
                {
                    Profiler.BeginSample("Collision Handle");
                    float startAngle = currentAngle;
                    bool isInside = true;
                    while(isInside)
                    {
                        currentAngle += iterationAngle;
                        loopingPos.x = wave.Origin.x + Mathf.Cos(currentAngle) * wave.Radius;
                        loopingPos.y = wave.Origin.y + Mathf.Sin(currentAngle) * wave.Radius;

                        isInside = IsCollisionAtLocation(loopingPos);

                        if (currentAngle > 10f)
                        {
                            _WavesToDestroy.Add(wave);
                            //Debug.Log("Wave Destroyed: Endless while predicted");
                            Profiler.EndSample();
                            return wave;
                        }
                    }
                    Profiler.EndSample();

                    SegmentWave(ref wave, startAngle, currentAngle);
                    return wave;
                }

            }

            return wave;
        }
        void SegmentWave(ref WaveSegment wave, float startAngle, float endAngle)
        {
            Profiler.BeginSample("Segment Wave");
            float currentStart = wave.StartAngleRadian;
            float currentEnd = wave.StartAngleRadian + wave.AngleSize;

            WaveSegment hitSegment = wave;
            if (wave.IsSegmented <= 0)
            {
                wave.StartAngleRadian = endAngle;
                if (wave.StartAngleRadian > 0) wave.StartAngleRadian -= 6.28f;
                wave.AngleSize = 6.28f - (endAngle - startAngle);
                wave.IsSegmented = 1;

                hitSegment.StartAngleRadian = endAngle;
                hitSegment.AngleSize = endAngle - startAngle;

            }
            else
            {
                if (endAngle > currentStart && startAngle < currentStart)
                {
                    wave.StartAngleRadian = endAngle;
                    wave.AngleSize -= endAngle - wave.StartAngleRadian;

                    //hitSegment.StartAngleRadian = hitSegment.StartAngleRadian;
                    hitSegment.AngleSize = endAngle - wave.StartAngleRadian;

                }
                else if (startAngle < currentEnd && endAngle > currentEnd)
                {
                    wave.AngleSize -= (currentEnd - startAngle);


                    hitSegment.StartAngleRadian = startAngle;
                    hitSegment.AngleSize = currentEnd - startAngle;
                }
                else
                {
                    wave.AngleSize = startAngle - currentStart;
                    SpawnWave(wave.Origin, wave.Radius, wave.Strength, endAngle, currentEnd - endAngle);

                    hitSegment.StartAngleRadian = startAngle;
                    hitSegment.AngleSize = endAngle - startAngle;
                }
            }

            Profiler.EndSample();
            if (_EnableBounceBack) ObjectHit(hitSegment);
        }

        public bool IsCollisionAtLocation(Vector2 normalisedTextureLocation)
        {
            int x = Mathf.RoundToInt(normalisedTextureLocation.x * _CollisionTexture.width);
            int y = Mathf.RoundToInt(normalisedTextureLocation.y * _CollisionTexture.height);

            //Debug.Log("Pos " + new Vector2Int(x,y));
            return _CollisionTexture.GetPixel(x, y).g > 0.5f;
        }

        Vector2 directionFromOrigin = new Vector2();
        void ObjectHit(WaveSegment hitSegment)
        {
            Profiler.BeginSample("Object Hit");
            float scalar = 0.01f;
            directionFromOrigin.x = Mathf.Cos(hitSegment.StartAngleRadian);
            directionFromOrigin.y = Mathf.Sin(hitSegment.StartAngleRadian);
                //new Vector2(Mathf.Cos(hitSegment.StartAngleRadian), Mathf.Sin(hitSegment.StartAngleRadian));

            //Vector2 pos = hitSegment.Origin + directionFromOrigin * (hitSegment.Radius-scalar);
            // SpawnWave(pos, 0f, hitSegment.Strength / 2, hitSegment.StartAngleRadian + hitSegment.AngleSize, hitSegment.StartAngleRadian + 6.28f);
            hitSegment.Origin = hitSegment.Origin + directionFromOrigin * (hitSegment.Radius - scalar);
            hitSegment.Strength = (hitSegment.Strength - 0.1f)/2;
            hitSegment.StartAngleRadian = hitSegment.StartAngleRadian + hitSegment.AngleSize;
            hitSegment.AngleSize = 6.28f - hitSegment.AngleSize;
            hitSegment.Radius = 0f;
            _Waves.Add(hitSegment);
            Profiler.EndSample();
            //SpawnWave(wave.Origin, wave.Radius, wave.Strength, endAngle, currentEnd - endAngle);
        }


        public void SpawnWave(Vector2 normalisedPosition)
        {
            if (IsCollisionAtLocation(normalisedPosition)) return;
            WaveSegment wave = _WaveManager.GetWave(normalisedPosition, 0f, _DefaultSpeed, _DefaultDecayPerSecond, _DefaultWaveStrenght, 0, 6.28f, _DefaultLineThickness, 0);
            if (IsValidSegment(wave))
            {
                _Waves.Add(wave);
            }
            else
            {
                _WaveManager.ReturnWave(wave);
            }
        }
        public void SpawnWave(Vector2 normalisedPosition, float startAngle, float angleSize)
        {
            if (IsCollisionAtLocation(normalisedPosition)) return;
            WaveSegment wave = _WaveManager.GetWave(normalisedPosition, 0f, _DefaultSpeed, _DefaultDecayPerSecond, _DefaultWaveStrenght, startAngle, angleSize, _DefaultLineThickness, 1);
            if (IsValidSegment(wave))
            {
                _Waves.Add(wave);
            }
            else
            {
                _WaveManager.ReturnWave(wave);
            }
        }
        public void SpawnWave(Vector2 normalisedPosition, float radius, float stength, float startAngle, float angleSize)
        {
            if (IsCollisionAtLocation(normalisedPosition)) return;
            WaveSegment wave = _WaveManager.GetWave(normalisedPosition, radius, _DefaultSpeed, _DefaultDecayPerSecond, stength, startAngle, angleSize, _DefaultLineThickness, 1);
            if (IsValidSegment(wave))
            {
                _Waves.Add(wave);
            }
            else
            {
                _WaveManager.ReturnWave(wave);
            }
        }
    }
}
