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
        [SerializeField] float _MaxSpeed;

        [Header("Debug Values")]
        [SerializeField] bool _DrawGrid =true;
        Vector3 _GridCellSize;

        ComputeBuffer _GridBufferOld;
        ComputeBuffer _GridBufferNew;

        #region CollisionTexture
        RenderTexture _CollisionTexture;
        public RenderTexture CollisionTexture
        {
            get
            {
                return _CollisionTexture;
            }
        }
        #endregion

        #region Kernels
        int _KernelBakeCollisionMap;
        #endregion


        Bounds _GridBounds = new Bounds();
        bool _Is1NewCollisionGrid = true;
        float[,,] _CollisionGrid1;
        float[,,] _CollisionGrid2;

        private void Start()
        {
            InitializeGrid();
            InitializeComputeBuffers();
            InitializeKernels();
            InitializeRenderTexture(ref _CollisionTexture, SceneData.Instance.SimData.TextureSize);


            if (SceneData.Instance.SimData.CollisionBaker == SimulationData.CollisionBakers.GridBake)
            {
                var waveProp = SceneData.Instance.WavePropagation;
                waveProp.CameraCollisionMapNew = CollisionTexture;

                _DebugMat1.SetTexture("_BaseMap", _CollisionTexture);
            }
        }

        private void OnDestroy()
        {
            _GridBufferOld.Dispose();
            _GridBufferNew.Dispose();
        }

        #region Initialize
        void InitializeGrid()
        {
            _CollisionGrid1 = new float[_AmountOfGridCells.x + 1, _AmountOfGridCells.y + 1, _AmountOfGridCells.z + 1];
            _CollisionGrid2 = new float[_AmountOfGridCells.x + 1, _AmountOfGridCells.y + 1, _AmountOfGridCells.z + 1];

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
            _GridBufferOld = new ComputeBuffer(_CollisionGrid1.Length, sizeof(float));
            _GridBufferNew = new ComputeBuffer(_CollisionGrid2.Length, sizeof(float));
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


            if (_DrawGrid)
            {
                if (_Is1NewCollisionGrid)
                {
                    DrawGrid(_CollisionGrid1);
                }
                else
                {
                    DrawGrid(_CollisionGrid2);
                }
            }
        }

        private void DrawGrid(in float[,,] grid)
        {
            for (int x = 0; x < _AmountOfGridCells.x + 1; x++)
            {
                for (int y = 0; y < _AmountOfGridCells.y + 1; y++)
                {
                    for (int z = 0; z < _AmountOfGridCells.z + 1; z++)
                    {
                        Vector3 temp = new Vector3(_GridCellSize.x * x, _GridCellSize.y * y, _GridCellSize.z * z);
                        Vector3 worldPos = _GridBounds.min + temp;
                        Debug.DrawLine(worldPos, worldPos + Vector3.up / 10, new Color(grid[x, y, z], 0, 0));
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (SceneData.Instance.SimData.CollisionBaker != SimulationData.CollisionBakers.GridBake) return;

            BakeTexture();
            _Is1NewCollisionGrid = !_Is1NewCollisionGrid;
            SceneData.Instance.WavePropagation.GridCollisionMap = CollisionTexture;

            if (_Is1NewCollisionGrid)
            {
                ClearGrid(ref _CollisionGrid1);
            }
            else
            {
                ClearGrid(ref _CollisionGrid2);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (SceneData.Instance.SimData.CollisionBaker != SimulationData.CollisionBakers.GridBake) return;
            if (_Is1NewCollisionGrid)
            {
                AddColliderToGrid(other, ref _CollisionGrid1);
            }
            else
            {
                AddColliderToGrid(other, ref _CollisionGrid2);
            }
            //Debug.Log(other);
        }

        public void AddColliderToGrid(Collider col, ref float[,,] grid)
        {
            float vel = col.attachedRigidbody.velocity.magnitude;
            vel /= _MaxSpeed;
            Mathf.Min(vel, 1f);

            Bounds bounds = col.bounds;
            
            Vector3 min = PhysicsHelpers.Vector3Max(bounds.min, _GridBounds.min);
            Vector3 max = PhysicsHelpers.Vector3Min(bounds.max, _GridBounds.max);

            Bounds overlappingBounds = new Bounds();
            overlappingBounds.SetMinMax(min, max);

            WaveInteractable waveInteractable = col.gameObject.GetComponent<WaveInteractable>();

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
                            grid[gridPos.x, gridPos.y, gridPos.z] = vel;
                        }
                    }
                }
            }
        }

        private void HandleCollisionInPoint(WaveInteractable waveInteractable, Vector3 worldPos)
        {
            float volumeOfOneGridCell = _GridCellSize.x * _GridCellSize.y * _GridCellSize.z;
            waveInteractable.ApplyForce(worldPos, volumeOfOneGridCell);
        }

        public void BakeTexture()
        {
            int textureSize = SceneData.Instance.SimData.TextureSize;
            if (_Is1NewCollisionGrid)
            {
                _GridBufferNew.SetData(_CollisionGrid1);
                _GridBufferOld.SetData(_CollisionGrid2);
            }
            else
            {
                _GridBufferNew.SetData(_CollisionGrid2);
                _GridBufferOld.SetData(_CollisionGrid1);
            }
            _CollisionBakerShader.SetBuffer(_KernelBakeCollisionMap, "CollisionGridNew", _GridBufferNew);
            _CollisionBakerShader.SetBuffer(_KernelBakeCollisionMap, "CollisionGridOld", _GridBufferOld);
            _CollisionBakerShader.SetTexture(_KernelBakeCollisionMap, "HeightMap", SceneData.Instance.WavePropagation.HeightMap);

            _CollisionBakerShader.SetTexture(_KernelBakeCollisionMap, "CollisionMap", _CollisionTexture);

            _CollisionBakerShader.SetVector("AmountOfGridCells", (Vector3)_AmountOfGridCells);
            _CollisionBakerShader.SetVector("GridPos", _GridBounds.min);
            _CollisionBakerShader.SetVector("GridSize", _GridBounds.size);
            _CollisionBakerShader.SetFloat("HeightScalar", SceneData.Instance.SimData.HeightScalar);
            _CollisionBakerShader.SetFloat("DefaultGridHeight", SceneData.Instance.SimData.DefaultDensityValue);
            _CollisionBakerShader.SetInt("TextureSize", textureSize);
            _CollisionBakerShader.Dispatch(_KernelBakeCollisionMap, textureSize / 8, textureSize / 8, 1);
        }
        public void ClearGrid(ref float[,,] grid)
        {
            for (int x = 0; x < _AmountOfGridCells.x +1; x++)
            {
                for (int y = 0; y < _AmountOfGridCells.y +1; y++)
                {
                    for (int z = 0; z < _AmountOfGridCells.z +1; z++)
                    {
                        grid[x, y, z] = 0f;
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
