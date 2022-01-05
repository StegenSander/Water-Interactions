using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class CollisionBaker : MonoBehaviour
    {
        [SerializeField] Vector3Int _AmountOfGridCells;
        Vector3 _GridCellSize;

        Bounds _GridBounds = new Bounds();
        float[,,] _CollisionGrid;

        private void Start()
        {
            _CollisionGrid = new float[_AmountOfGridCells.x +1, _AmountOfGridCells.y +1, _AmountOfGridCells.z +1];

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

        private void Update()
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
            ClearGrid();
        }

        private void OnTriggerStay(Collider other)
        {
            AddColliderToGrid(other);
            Debug.Log(other);
        }

        public void AddColliderToGrid(Collider col)
        {
            Bounds bounds = col.bounds;

            //Debug.Log(col.bounds);

            Vector3 min = Vector3Max(bounds.min, _GridBounds.min);
            Vector3 max = Vector3Min(bounds.max, _GridBounds.max);
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
        public Vector3 Vector3Min(Vector3 vec1, Vector3 vec2)
        {
            Vector3 result = new Vector3();
            result.x = Mathf.Min(vec1.x, vec2.x);
            result.y = Mathf.Min(vec1.y, vec2.y);
            result.z = Mathf.Min(vec1.z, vec2.z);
            return result;
        }

        public Vector3 Vector3Max(Vector3 vec1, Vector3 vec2)
        {
            Vector3 result = new Vector3();
            result.x = Mathf.Max(vec1.x, vec2.x);
            result.y = Mathf.Max(vec1.y, vec2.y);
            result.z = Mathf.Max(vec1.z, vec2.z);
            return result;
        }
        public Vector3Int Vector3Min(Vector3Int vec1, Vector3Int vec2)
        {
            Vector3Int result = new Vector3Int();
            result.x = Mathf.Min(vec1.x, vec2.x);
            result.y = Mathf.Min(vec1.y, vec2.y);
            result.z = Mathf.Min(vec1.z, vec2.z);
            return result;
        }

        public Vector3Int Vector3Max(Vector3Int vec1, Vector3Int vec2)
        {
            Vector3Int result = new Vector3Int();
            result.x = Mathf.Max(vec1.x, vec2.x);
            result.y = Mathf.Max(vec1.y, vec2.y);
            result.z = Mathf.Max(vec1.z, vec2.z);
            return result;
        }

        public Vector3Int WorldPosToGridCell(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();
            Vector3 normalisedPos = pos - _GridBounds.min;

            result.x = Mathf.RoundToInt(normalisedPos.x / _GridCellSize.x);
            result.y = Mathf.RoundToInt(normalisedPos.y / _GridCellSize.y);
            result.z = Mathf.RoundToInt(normalisedPos.z / _GridCellSize.z);

            return result;
        }

        //public Vector3 RoundToGrid(Vector3 pos)
        //{

        //}
        #endregion
    }
}
