using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class WaveSpawner3D : MonoBehaviour
    {
        [SerializeField] GameObject _BodyOfWater;
        NavierStokesPropagation _WavePropagation;
        // Start is called before the first frame update
        void Start()
        {
            _WavePropagation = FindObjectOfType<NavierStokesPropagation>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    if (hit.collider.gameObject == _BodyOfWater)
                    {
                        Debug.Log(hit.textureCoord);
                        Debug.Log(hit.textureCoord2);
                        Debug.Log(hit.point);
                        _WavePropagation.SpawnWave(hit.textureCoord);
                    }
                }
            }
        }
    }
}
