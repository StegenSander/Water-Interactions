using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace WaterInteraction
{
    [RequireComponent(typeof(Rigidbody))] [RequireComponent(typeof(Collider))]
    public class WaveInteractable : MonoBehaviour
    {
        [SerializeField] int _CollisionSamplesPerMeter = 5;
        [SerializeField] float _MinDistanceBetweenCollisionPoints;

        int _AmountOfCollisionPoints;
        List<Transform> _CollisionPoints;

        float _Volume = 1;
        Rigidbody _Rigidbody;
        Collider _Collider;
        int _WaterLayerIndex;

        bool _isInWater;
        Collider _CurrentBodyOfWater;
        WaterForceHandler _CurrentWaterForceHandler;

        // Start is called before the first frame update
        void Start()
        {
            _Rigidbody = GetComponent<Rigidbody>();
            _Collider = GetComponent<Collider>();
            _WaterLayerIndex = LayerMask.NameToLayer("CustomWater");

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _Volume = VolumeCalcManager.Instance.GetVolume(meshFilter.mesh, transform.localScale);

            List<Vector3> points = new List<Vector3>();
            PhysicsHelpers.CalculateWaterCollisionPoints(_CollisionSamplesPerMeter, _MinDistanceBetweenCollisionPoints, _Collider, ref points);
            _AmountOfCollisionPoints = Mathf.Max(points.Count, 1);
            _CollisionPoints = new List<Transform>(_AmountOfCollisionPoints);

            GameObject obj = new GameObject();
            foreach (Vector3 pos in points)
            {
                GameObject collisionPointObject = Instantiate(obj, pos, Quaternion.identity, transform);
                _CollisionPoints.Add(collisionPointObject.transform);
            }
            Destroy(obj);
        }

        /// <summary>
        /// This function is an estimate
        /// </summary>
        /// <param name="WaterBounds"></param>
        /// <returns></returns>
        private float GetPercentageBoundingBoxInWater(Bounds WaterBounds)
        {
            float boundsVolumeInWater = PhysicsHelpers.GetVolumeOfBounds(PhysicsHelpers.GetOverlappingBounds(WaterBounds, _Collider.bounds));
            float boundsVolume = PhysicsHelpers.GetVolumeOfBounds(_Collider.bounds);
            float percentageInWater = boundsVolumeInWater / boundsVolume;
            //Debug.Log("Volume: " + boundsVolume + "Volume in Water" + boundsVolumeInWater + "Perc: " + percentageInWater);
            return percentageInWater;
        }

        private void FixedUpdate()
        {
            if (!_CurrentWaterForceHandler) return;

            Profiler.BeginSample("ApplyingForceToObject");
            float volumePerc = 1f / _AmountOfCollisionPoints;
            foreach (Transform t in _CollisionPoints)
            {
                if (_CurrentWaterForceHandler.GetPosToSurfaceOffset(t.position, out float offset))
                {
                    Vector3 force = Vector3.up * PhysicsHelpers.CalculateFluidForce(_Volume* volumePerc);
                    _Rigidbody.AddForceAtPosition(force, t.position, ForceMode.Force);
                }
            }
            Profiler.EndSample();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _WaterLayerIndex)
            {
                _isInWater = true;
                _CurrentBodyOfWater = other;
                _CurrentWaterForceHandler = other.gameObject.GetComponent<WaterForceHandler>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _WaterLayerIndex)
            {
                _isInWater = false;
                _CurrentBodyOfWater = null;
                _CurrentWaterForceHandler = null;
            }
        }

        public void ApplyForce(Vector3 worldPosition, float volume)
        {
            Vector3 force = Vector3.up * PhysicsHelpers.CalculateFluidForce(volume);
            _Rigidbody.AddForceAtPosition(force, worldPosition, ForceMode.Force);
        }
    }
}
