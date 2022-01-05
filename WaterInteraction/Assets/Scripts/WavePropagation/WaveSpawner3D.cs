using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class WaveSpawner3D : MonoBehaviour
    {
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
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("CustomWater"))
                    {
                        if (hit.collider.GetType() == typeof(BoxCollider))
                        {
                            BoxCollider boxCollider = (BoxCollider)hit.collider;

                            Vector3 actualSize = boxCollider.size;
                            actualSize.Scale(hit.collider.gameObject.transform.localScale);

                            Vector3 actualCenter = boxCollider.center + hit.collider.gameObject.transform.position;

                            Vector3 minTop = actualCenter + new Vector3(-actualSize.x/2, actualSize.y/2, -actualSize.z/2);

                            Vector3 hitPos = hit.point;
                            hitPos -= minTop;
                            hitPos = new Vector3(hitPos.x / (actualSize.x), 0, hitPos.z / (actualSize.z));


                            _WavePropagation.SpawnWave(new Vector2(1 - hitPos.x, 1 - hitPos.z));
                            //Debug.Log("HitPos: " + hitPos);

                        }
                        else if (hit.collider.GetType() == typeof(MeshCollider))
                        {
                            _WavePropagation.SpawnWave(hit.textureCoord);
                        }
                        else
                        {
                            Debug.LogWarning(hit.collider.GetType() + " Shape not implemented");
                        }
                    }
                }
            }
        }
    }
}
