using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    [RequireComponent(typeof(Rigidbody))] [RequireComponent(typeof(Collider))]
    public class WaveInteractable : MonoBehaviour
    {
        float _Volume = 1;
        Rigidbody _Rigidbody;
        Collider _Collider;
        int _WaterLayerIndex;

        bool _isInWater;
        Collider _CurrentBodyOfWater;

        // Start is called before the first frame update
        void Start()
        {
            _Rigidbody = GetComponent<Rigidbody>();
            _Collider = GetComponent<Collider>();
            _WaterLayerIndex = LayerMask.NameToLayer("CustomWater");

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _Volume = VolumeCalcManager.Instance.GetVolume(meshFilter.mesh, transform.localScale);
        }

        // Update is called once per frame
        void Update()
        {
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
            if (_isInWater)
            {
                float volumePerc = GetPercentageBoundingBoxInWater(_CurrentBodyOfWater.bounds);
                Vector3 force = Vector3.up * PhysicsHelpers.CalculateFluidForce(volumePerc * _Volume);
                _Rigidbody.AddForce(force, ForceMode.Force);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _WaterLayerIndex)
            {
                _isInWater = true;
                _CurrentBodyOfWater = other;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _WaterLayerIndex)
            {
                _isInWater = false;
                _CurrentBodyOfWater = null;
            }
        }
    }
}
