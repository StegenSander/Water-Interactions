using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class WaveSegmentObjectManager : MonoBehaviour
    {
        [SerializeField] int _AmountOfStartObject;
        int _CreatedObjects;

        Stack<WaveSegment> _InactiveStack = new Stack<WaveSegment>();
        // Start is called before the first frame update
        void Start()
        {
            CreateWaveSegments(_AmountOfStartObject);
        }

        void CreateWaveSegments(int amount)
        {
            _CreatedObjects += amount;
            for (int i = 0; i < amount; i++)
            {
                _InactiveStack.Push(new WaveSegment());
            }
            Debug.Log("WaveSegments created: " + amount);

        }

        public void ReturnWave(WaveSegment wave)
        {
            _InactiveStack.Push(wave);
        }

        public WaveSegment GetWave(Vector2 normalisedPosition, float radius, float speed, float strenghtDecay
            , float strength, float startAngle, float angleSize, float waveThickness, int isSegmented)
        {
            if (_InactiveStack.Count <= 0)
            {
                CreateWaveSegments(_CreatedObjects + 1);
            }
            WaveSegment wave = _InactiveStack.Pop();
            wave.Origin = normalisedPosition;
            wave.Radius = radius;
            wave.Speed = speed;
            wave.StrenghtDecay = strenghtDecay;
            wave.Strength = strength;
            wave.StartAngleRadian = startAngle;
            wave.AngleSize = angleSize;
            wave.WaveThickness = waveThickness;
            wave.IsSegmented = isSegmented;
            return wave;
        }
    }
}
