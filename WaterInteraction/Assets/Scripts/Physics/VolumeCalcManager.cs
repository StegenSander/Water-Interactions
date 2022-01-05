using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{

    public class VolumeCalcManager : Singleton<VolumeCalcManager>
    {
        Dictionary<Mesh, float> _VolumeDictionary = new Dictionary<Mesh, float>();


        //Non uniformly scale meshes will not get cached
        public float GetVolume(Mesh mesh, Vector3 scale)
        {
            if (scale.x == scale.y && scale.y == scale.z)
            {
                return GetVolumeUniformScaled(mesh, scale.x);
            }
            else
            {
                return GetVolumeNonUniformScaled(mesh, scale);
            }
        }

        public float GetVolumeUniformScaled(Mesh mesh, float scale)
        {
            float scaleCubed = scale * scale * scale;

            if (!_VolumeDictionary.ContainsKey(mesh))
            {
                _VolumeDictionary[mesh] = PhysicsHelpers.CalculateVolumeOfMesh(mesh);
            }

            return _VolumeDictionary[mesh] * scaleCubed;
        }

        public float GetVolumeNonUniformScaled(Mesh mesh, Vector3 scale)
        {
            return PhysicsHelpers.CalculateVolumeOfMesh(mesh, scale);
        }
    }


}
