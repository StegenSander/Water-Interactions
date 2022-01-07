using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SimulationData", menuName = "ScriptableObjects/SimulationData", order = 1)]
public class SimulationData : ScriptableObject
{
    public enum CollisionBakers
    {
        CameraBake,
        GridBake,
    }
    [Header("TextureParam")]
    public int TextureSize = 32;

    [Header("SurfaceParam")]
    public float VertexHeightToBeSurface = 0.5f;
    public float HeightScalar = 3f;

    [Header("FluidParam")]
    public float DefaultDensityValue = 0.5f;
    public float FluidDensity = 1;

    [Header("CollisionParam")]
    public CollisionBakers CollisionBaker;
}