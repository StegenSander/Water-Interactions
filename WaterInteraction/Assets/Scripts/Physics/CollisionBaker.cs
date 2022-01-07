using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class CollisionBaker : MonoBehaviour
    {

        [SerializeField] Material _DebugMat1;
        [SerializeField] Material _DebugMat2;

        [Header("Core Values")]
        [SerializeField] ComputeShader _CollisionBakerShader;
        [SerializeField] Vector3Int _AmountOfGridCells;

        [Header("Debug Values")]
        [SerializeField] bool _DrawGrid =true;
        Vector3 _GridCellSize;

        ComputeBuffer _GridBuffer;

        #region CollisionTexture
        RenderTexture _CollisionTexture1;
        RenderTexture _CollisionTexture2;
        bool _Is1NewCollisionTexture = true;
        public RenderTexture NewCollisionTexture
        {
            get
            {
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
        #endregion

        #region Kernels
        int _KernelBakeCollisionMap;
        #endregion

        Bounds _GridBounds = new Bounds();
        float[,,] _CollisionGrid;

        private void Start()
        {
            InitializeGrid();
            InitializeComputeBuffers();
            InitializeKernels();
            InitializeRenderTexture(ref _CollisionTexture1, SceneData.Instance.SimData.TextureSize);
            InitializeRenderTexture(ref _CollisionTexture2, SceneData.Instance.SimData.TextureSize);


            if (SceneData.Instance.SimData.CollisionBaker == SimulationData.CollisionBakers.GridBake)
            {
                var waveProp = SceneData.Instance.WavePropagation;
                waveProp.DynamicCollisionNew = NewCollisionTexture;
                waveProp.DynamicCollisionOld = OldCollisionTexture;

                _DebugMat1.SetTexture("_BaseMap", _CollisionTexture1);
                _DebugMat2.SetTexture("_BaseMap", _CollisionTexture2);
            }
        }

        private void OnDestroy()
        {
            _GridBuffer.Dispose();
        }

        #region Initialize
        void InitializeGrid()
        {
            _CollisionGrid = new float[_AmountOfGridCells.x + 1, _AmountOfGridCells.y + 1, _AmountOfGridCells.z + 1];

            BoxCollider col = GetComponent<BoxCollider>();
            Vector3 gridSize = col.size;
            gridSize.Scale(transform.localScale);
            Vector3 gridBottomLeftPos = (transform.position + col.center) - gridSize / 2;

            _GridBounds.SetMinMax(gridBottomLeftPos, gridBottomLeftPos + gridSize);
            _GridCellSize =
                new Vector3(_GridBounds.size.x / _AmountOfGridCells.x
                , _GridBounds.size.y / _AmountOfGridCells.y
                , _GridBounds.size.z / _AmountOfGridCells.z);
        }

        void InitializeComputeBuffers()
        {
            _GridBuffer = new ComputeBuffer(_CollisionGrid.Length, sizeof(float));
        }

        void InitializeKernels()
        {
            _KernelBakeCollisionMap = _CollisionBakerShader.FindKernel("BakeCollisionMap");
        }

        void InitializeRenderTexture(ref RenderTexture texture, int size)
        {
            texture = new RenderTexture(size,size, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.sRGB);
            texture.filterMode = FilterMode.Point;
            texture.name = "TargetTexture (Generated)";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.enableRandomWrite = true;
            texture.Create();
        }
        #endregion

        private void Update()
        {
            if (SceneData.Instance.SimData.CollisionBaker != SimulationData.CollisionBakers.GridBake) return;

            if (_DrawGrid) DrawGrid();
        }

        private void DrawGrid()
        {
            for (int x = 0; x < _AmountOfGridCells.x + 1; x++)
            {
                for (int y = 0; y < _AmountOfGridCells.y + 1; y++)
                {
                    for (int z = 0; z < _AmountOfGridCells.z + 1; z++)
                    {
                        Vector3 temp = new Vector3(_GridCellSize.x * x, _GridCellSize.y * y, _GridCellSize.z * z);
                        Vector3 worldPos = _GridBounds.min + temp;
                        Debug.DrawLine(worldPos, worldPos + Vector3.up / 10, new Color(_CollisionGrid[x, y, z], 0, 0));
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (SceneData.Instance.SimData.CollisionBaker != SimulationData.CollisionBakers.GridBake) return;

            BakeTexture();
            SceneData.Instance.WavePropagation.DynamicCollisionNew = NewCollisionTexture;
            SceneData.Instance.WavePropagation.DynamicCollisionOld = OldCollisionTexture;
            _Is1NewCollisionTexture = !_Is1NewCollisionTexture;

            ClearGrid();
        }

        private void OnTriggerStay(Collider other)
        {
            if (SceneData.Instance.SimData.CollisionBaker != SimulationData.CollisionBakers.GridBake) return;

            AddColliderToGrid(other);
            Debug.Log(other);
        }

        public void AddColliderToGrid(Collider col)
        {
            Bounds bounds = col.bounds;
            
            Vector3 min = PhysicsHelpers.Vector3Max(bounds.min, _GridBounds.min);
            Vector3 max = PhysicsHelpers.Vector3Min(bounds.max, _GridBounds.max);

            Bounds overlappingBounds = new Bounds();
            overlappingBounds.SetMinMax(min, max);

            //Can Cause rounding issues, if wave are ever offset, this will be the issue
            for (float x = overlappingBounds.min.x; x < overlappingBounds.max.x; x += _GridCellSize.x)
            {
                for (float y = overlappingBounds.min.y; y < overlappingBounds.max.y; y += _GridCellSize.y)
                {
                    for (float z = overlappingBounds.min.z; z < overlappingBounds.max.z; z += _GridCellSize.z)
                    {
                        Vector3 worldPos = new Vector3(x, y, z);
                        Vector3Int gridPos = WorldPosToGridCell(worldPos);
                        if (col.ClosestPoint(worldPos) == worldPos)
                        {
                            _CollisionGrid[gridPos.x, gridPos.y, gridPos.z] = 1f;
                        }
                    }
                }
            }
        }

        public void BakeTexture()
        {
            int textureSize = SceneData.Instance.SimData.TextureSize;
            _GridBuffer.SetData(_CollisionGrid);
            _CollisionBakerShader.SetBuffer(_KernelBakeCollisionMap, "CollisionGrid", _GridBuffer);
            _CollisionBakerShader.SetTexture(_KernelBakeCollisionMap, "HeightMap", SceneData.Instance.WavePropagation.HeightMap);

            if (_Is1NewCollisionTexture)
            {
                _CollisionBakerShader.SetTexture(_KernelBakeCollisionMap, "CollisionMap", _CollisionTexture1);
            }
            else
            {
                _CollisionBakerShader.SetTexture(_KernelBakeCollisionMap, "CollisionMap", _CollisionTexture2);
            }

            _CollisionBakerShader.SetVector("AmountOfGridCells", (Vector3)_AmountOfGridCells);
            _CollisionBakerShader.SetVector("GridPos", _GridBounds.min);
            _CollisionBakerShader.SetVector("GridSize", _GridBounds.size);
            _CollisionBakerShader.SetFloat("HeightScalar", SceneData.Instance.SimData.HeightScalar);
            _CollisionBakerShader.SetFloat("DefaultGridHeight", SceneData.Instance.SimData.DefaultDensityValue);
            _CollisionBakerShader.SetInt("TextureSize", textureSize);
            _CollisionBakerShader.Dispatch(_KernelBakeCollisionMap, textureSize / 8, textureSize / 8, 1);
        }
        public void ClearGrid()
        {
            for (int x = 0; x < _AmountOfGridCells.x +1; x++)
            {
                for (int y = 0; y < _AmountOfGridCells.y +1; y++)
                {
                    for (int z = 0; z < _AmountOfGridCells.z +1; z++)
                    {
                        _CollisionGrid[x, y, z] = 0f;
                    }
                }
            }
        }

        #region MathHelpers
        public Vector3Int WorldPosToGridCell(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();
            Vector3 normalisedPos = pos - _GridBounds.min;

            result.x = Mathf.RoundToInt(normalisedPos.x / _GridCellSize.x);
            result.y = Mathf.RoundToInt(normalisedPos.y / _GridCellSize.y);
            result.z = Mathf.RoundToInt(normalisedPos.z / _GridCellSize.z);

            return result;
        }
        #endregion
    }
}
