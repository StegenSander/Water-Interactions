using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

public class ProfilerDataCollector : MonoBehaviour
{
    [SerializeField] bool _RunSamples;
    [SerializeField] int _AmountOfSample = 10;
    int _RemainingSample = 0;

    List<float> _CollisionBakeSamples = new List<float>();
    static public CustomSampler CollisionBakeSampler;
    Recorder _CollisionBakeRecorder;

    List<float> _AddingCollidersSamples = new List<float>();
    static public CustomSampler AddingCollidersSampler;
    Recorder _AddingCollidersRecorder;

    List<float> _WaterForceSamples = new List<float>();
    static public CustomSampler WaterForceSampler;
    Recorder _WaterForceRecorder;

    List<float> _NewVelocitiesSamples = new List<float>();
    static public CustomSampler NewVelocitiesSampler;
    Recorder _NewVelocitiesRecorder;

    List<float> _DiffuseTextureSamples = new List<float>();
    static public CustomSampler DiffuseTextureSampler;
    Recorder _DiffuseTextureRecorder;

    List<float> _UpdateVelSamples = new List<float>();
    static public CustomSampler UpdateVelSampler;
    Recorder _UpdateVelRecorder;

    List<float> _ConvertSamples = new List<float>();
    static public CustomSampler ConvertSampler;
    Recorder _ConvertRecorder;

    // Start is called before the first frame update
    void Start()
    {
        CollisionBakeSampler = CustomSampler.Create("BakeCollision");
        _CollisionBakeRecorder = Sampler.Get("BakeCollision").GetRecorder();
        _CollisionBakeRecorder.enabled = true;

        AddingCollidersSampler = CustomSampler.Create("AddingColliders");
        _AddingCollidersRecorder = Sampler.Get("AddingColliders").GetRecorder();
        _AddingCollidersRecorder.enabled = true;

        WaterForceSampler = CustomSampler.Create(" WaterForce");
        _WaterForceRecorder = Sampler.Get(" WaterForce").GetRecorder();
        _WaterForceRecorder.enabled = true;

        NewVelocitiesSampler = CustomSampler.Create("NewVelocities");
        _NewVelocitiesRecorder = Sampler.Get("NewVelocities").GetRecorder();
        _NewVelocitiesRecorder.enabled = true;

        DiffuseTextureSampler = CustomSampler.Create("DiffuseTexture");
        _DiffuseTextureRecorder = Sampler.Get("DiffuseTexture").GetRecorder();
        _DiffuseTextureRecorder.enabled = true;

        UpdateVelSampler = CustomSampler.Create("UpdateVel");
        _UpdateVelRecorder = Sampler.Get("UpdateVel").GetRecorder();
        _UpdateVelRecorder.enabled = true;

        ConvertSampler = CustomSampler.Create("Convert");
        _ConvertRecorder = Sampler.Get("Convert").GetRecorder();
        _ConvertRecorder.enabled = true;

    }

    // Update is called once per frame
    void Update()
    {
        if (_RemainingSample > 0)
        {
            if (_CollisionBakeRecorder.sampleBlockCount < 1) return;
            _RemainingSample--;

            _CollisionBakeSamples.Add(_CollisionBakeRecorder.elapsedNanoseconds /( _CollisionBakeRecorder.sampleBlockCount * 1000000f));
            _AddingCollidersSamples.Add(_AddingCollidersRecorder.elapsedNanoseconds / (_AddingCollidersRecorder.sampleBlockCount * 1000000f));
            _WaterForceSamples.Add(_WaterForceRecorder.elapsedNanoseconds / (_WaterForceRecorder.sampleBlockCount * 1000000f));
            _NewVelocitiesSamples.Add(_NewVelocitiesRecorder.elapsedNanoseconds / (_NewVelocitiesRecorder.sampleBlockCount/2 * 1000000f));

            _DiffuseTextureSamples.Add(_DiffuseTextureRecorder.elapsedNanoseconds / 1000000f);
            _UpdateVelSamples.Add(_UpdateVelRecorder.elapsedNanoseconds / 1000000f);
            _ConvertSamples.Add(_ConvertRecorder.elapsedNanoseconds / 1000000f);

            if (_RemainingSample == 0)
            {
                DoneCollectionSamples();
            }
        }

        if (_RunSamples)
        {
            _RemainingSample = _AmountOfSample;
            _RunSamples = false;

            _CollisionBakeSamples.Clear();
            _AddingCollidersSamples.Clear();
            _WaterForceSamples.Clear();
            _NewVelocitiesSamples.Clear();
            _DiffuseTextureSamples.Clear();
            _UpdateVelSamples.Clear();
            _ConvertSamples.Clear();
        }
    }

    void DoneCollectionSamples()
    {
        Debug.Log("---------------------------------");
        Debug.Log("START OF PERFORMANCE SAMPLES");
        Debug.Log("CollisionBake Average: " + CalculateAverage(_CollisionBakeSamples));
        Debug.Log("Adding Colliders Average: " + CalculateAverage(_AddingCollidersSamples));
        Debug.Log("WaterForce Average: " + CalculateAverage(_WaterForceSamples));
        Debug.Log("New Velocities Average: " + CalculateAverage(_NewVelocitiesSamples));
        Debug.Log("Diffuse Texture Average: " + CalculateAverage(_DiffuseTextureSamples));
        Debug.Log("Update vel Average: " + CalculateAverage(_UpdateVelSamples));
        Debug.Log("Conver Average: " + CalculateAverage(_ConvertSamples));
        Debug.Log("END OF PERFORMANCE SAMPLES");
        Debug.Log("---------------------------------");
    }

    float CalculateAverage(List<float> list)
    {
        return (list.Sum() - list.Min() - list.Max()) / (list.Count - 2);
    }
}
