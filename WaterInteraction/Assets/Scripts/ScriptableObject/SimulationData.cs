using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SimulationData", menuName = "ScriptableObjects/SimulationData", order = 1)]
public class SimulationData : ScriptableObject
{
    public int TextureSize = 32;
    public float VertexHeightToBeSurface = 0.5f;
    public float FluidDensity = 1;
}