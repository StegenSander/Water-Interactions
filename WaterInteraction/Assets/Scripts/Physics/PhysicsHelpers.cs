using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    static public class PhysicsHelpers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="volume">in m^3 or cm^3</param>
        /// <param name="fluidDensity">in kg/m^3 or g/cm^3 MUST MATCH VOLUME</param>
        /// <param name="gravity">in m/s^2</param>
        /// <returns></returns>
        static public float CalculateFluidForce(float volume, float fluidDensity = 1, float gravity = 9.81f)
        {
            return fluidDensity * volume * gravity;
        }

        #region VolumeCalculation
        static public float GetVolumeOfBounds(Bounds bounds)
        {
            return bounds.size.x * bounds.size.y * bounds.size.z;
        }
        #endregion
        //Based on https://stackoverflow.com/questions/1406029/how-to-calculate-the-volume-of-a-3d-mesh-object-the-surface-of-which-is-made-up
        //Call through VolumeCalcManager for optimal results
        #region MeshVolumeCalculation
        static public float CalculateVolumeOfMesh(Mesh mesh)
        {
            float volume = 0f;

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float tempVolume = CalculateVolumeOfTriangle(mesh.vertices[triangles[i]], mesh.vertices[triangles[i + 1]], mesh.vertices[triangles[i + 2]]);
                volume += Mathf.Abs(tempVolume);
            }

            return volume;
        }
        static public float CalculateVolumeOfMesh(Mesh mesh, Vector3 scale)
        {
            float volume = 0f;

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 vertex1 = mesh.vertices[triangles[i]];
                vertex1.Scale(scale);
                Vector3 vertex2 = mesh.vertices[triangles[i + 1]];
                vertex2.Scale(scale);
                Vector3 vertex3 = mesh.vertices[triangles[i + 2]];
                vertex3.Scale(scale);

                float tempVolume = CalculateVolumeOfTriangle(vertex1, vertex2, vertex3);
                volume += Mathf.Abs(tempVolume);
            }

            return volume;
        }
        static private float CalculateVolumeOfTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            var v321 = vertex3.x * vertex2.y * vertex1.z;
            var v231 = vertex2.x * vertex3.y * vertex1.z;
            var v312 = vertex3.x * vertex1.y * vertex2.z;
            var v132 = vertex1.x * vertex3.y * vertex2.z;
            var v213 = vertex2.x * vertex1.y * vertex3.z;
            var v123 = vertex1.x * vertex2.y * vertex3.z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }
        #endregion

        #region CollisionCalculations
        static public void CalculateWaterCollisionPoints(int amountOfSamplesPerMeter,float minDistanceBetweenPoint, Collider col,ref List<Vector3> OUTCollisionPoints)
        {
            //if (col.GetType() == typeof(BoxCollider)) //Optimasation possible fir every specific collider type
            //{

            //}
            //else //Default should work for every collider
            {
                Bounds bounds = col.bounds;
                float sampleSize = 1f / amountOfSamplesPerMeter;
                Vector3 sampleCount = new Vector3(amountOfSamplesPerMeter * bounds.size.x
                    , amountOfSamplesPerMeter * bounds.size.y
                    , amountOfSamplesPerMeter * bounds.size.z);

                Debug.Log(bounds);


                List<Vector3> tempPoints = new List<Vector3>(Mathf.RoundToInt(sampleCount.x * sampleCount.y * sampleCount.z/2));

                for (int x = 0; x < sampleCount.x; x++)
                {
                    for (int y = 0; y < sampleCount.y; y++)
                    {
                        for (int z = 0; z < sampleCount.z; z++)
                        {
                            Vector3 worldBoundPos = bounds.min + sampleSize * new Vector3(x, y, z);
                            Vector3 closestPos = col.ClosestPoint(worldBoundPos);

                            if (closestPos != worldBoundPos) //If it's not the original point it means that the position got clamped to the collider
                            {
                                if (!OUTCollisionPoints.Exists(
                                    vec => (vec - closestPos).sqrMagnitude < minDistanceBetweenPoint * minDistanceBetweenPoint)
                                    )
                                {
                                    OUTCollisionPoints.Add(closestPos);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region MathHelpers
        static public Bounds GetOverlappingBounds(Bounds lhs, Bounds bounds)
        {
            Vector3 min = Vector3Max(lhs.min, bounds.min);
            Vector3 max = Vector3Min(lhs.max, bounds.max);
            Bounds overlappingBounds = new Bounds();
            overlappingBounds.SetMinMax(min, max);
            return overlappingBounds;
        }

        static public Vector3 Vector3Min(Vector3 vec1, Vector3 vec2)
        {
            Vector3 result = new Vector3();
            result.x = Mathf.Min(vec1.x, vec2.x);
            result.y = Mathf.Min(vec1.y, vec2.y);
            result.z = Mathf.Min(vec1.z, vec2.z);
            return result;
        }

        static public Vector3 Vector3Max(Vector3 vec1, Vector3 vec2)
        {
            Vector3 result = new Vector3();
            result.x = Mathf.Max(vec1.x, vec2.x);
            result.y = Mathf.Max(vec1.y, vec2.y);
            result.z = Mathf.Max(vec1.z, vec2.z);
            return result;
        }
        static public Vector3Int Vector3Min(Vector3Int vec1, Vector3Int vec2)
        {
            Vector3Int result = new Vector3Int();
            result.x = Mathf.Min(vec1.x, vec2.x);
            result.y = Mathf.Min(vec1.y, vec2.y);
            result.z = Mathf.Min(vec1.z, vec2.z);
            return result;
        }

        static public Vector3Int Vector3Max(Vector3Int vec1, Vector3Int vec2)
        {
            Vector3Int result = new Vector3Int();
            result.x = Mathf.Max(vec1.x, vec2.x);
            result.y = Mathf.Max(vec1.y, vec2.y);
            result.z = Mathf.Max(vec1.z, vec2.z);
            return result;
        }
        #endregion
    }

}