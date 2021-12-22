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
                int stride = sizeof(float) * 9; //SizeOfWaveSegment
                int count = 1024;
                _WaveSegmentBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            }
        }

        public void DrawAllWaveSegments(RenderTexture texture, List<WaveSegment> waveSegments)
        {
            _WaveSegmentBuffer.SetData(waveSegments);
            _DrawWaveSegments.SetBuffer(_KernelDrawWaveSegments, "WaveSegments", _WaveSegmentBuffer);
            _DrawWaveSegments.SetInt("WaveSegmentCount", waveSegments.Count);
            _DrawWaveSegments.SetInt("TextureSize",texture.width);
            _DrawWaveSegments.SetTexture(_KernelDrawWaveSegments, "TargetTexture", texture);
            _DrawWaveSegments.Dispatch(_KernelDrawWaveSegments, texture.width / 8, texture.height / 8, 1);
        }
    }
}
