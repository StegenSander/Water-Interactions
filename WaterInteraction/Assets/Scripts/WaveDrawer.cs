using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WaterInteraction
{
    public class WaveDrawer : MonoBehaviour
    {
        [SerializeField] ComputeShader _DrawWaveSegments;
        int _KernelDrawWaveSegments;
        ComputeBuffer _WaveSegmentBuffer;

        // Start is called before the first frame update
        void Start()
        {
            if (!_DrawWaveSegments)
            {
                Debug.LogWarning("ComputeShader not assigned");
                enabled = false;
                return;
            }
            InitializeShader();
            InitializeBuffers();
        }

        void InitializeShader()
        {
            _KernelDrawWaveSegments = _DrawWaveSegments.FindKernel("DrawWaveSegments");
        }

        void InitializeBuffers()
        {
            {
                int stride = sizeof(float) * 9 + sizeof(int); //SizeOfWaveSegment
                int count = 1024;
                _WaveSegmentBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            }
        }

        private void OnDestroy()
        {
            _WaveSegmentBuffer.Release();
        }

        public void DrawAllWaveSegments(RenderTexture texture, Texture2D collisionTexture,List<WaveSegment> waveSegments)
        {
            if (_WaveSegmentBuffer.count < waveSegments.Count)
                Debug.LogWarning("Wave segment buffer out of space, skipping draw of: " + (waveSegments.Count - _WaveSegmentBuffer.count) + " waves");
            _WaveSegmentBuffer.SetData(waveSegments,0,0,Mathf.Min(waveSegments.Count, _WaveSegmentBuffer.count));
            _DrawWaveSegments.SetBuffer(_KernelDrawWaveSegments, "WaveSegments", _WaveSegmentBuffer);
            _DrawWaveSegments.SetInt("WaveSegmentCount", waveSegments.Count);
            _DrawWaveSegments.SetInt("TargetTextureSize", texture.width);
            _DrawWaveSegments.SetTexture(_KernelDrawWaveSegments, "TargetTexture", texture);
            _DrawWaveSegments.SetTexture(_KernelDrawWaveSegments, "CollisionTexture", collisionTexture);
            _DrawWaveSegments.SetInt("CollisionTextureSize", collisionTexture.width);
            _DrawWaveSegments.Dispatch(_KernelDrawWaveSegments, texture.width / 8, texture.height / 8, 1);
        }
    }
}
