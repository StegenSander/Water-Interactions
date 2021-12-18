using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WaterInteraction;

namespace WaterInteraction
{

    public class WaveSpawner : MonoBehaviour
    {
        WavePropagation _WavePropagation;
        [SerializeField] Image _TargetField;
        Rect _TargetArea;
        // Start is called before the first frame update
        void Start()
        {
            _WavePropagation = FindObjectOfType<WavePropagation>();
            InitializeTargetField();
        }

        void InitializeTargetField()
        {
            RectTransform rt = _TargetField.gameObject.GetComponent<RectTransform>();
            _TargetArea = rt.rect;
            _TargetArea.position += (Vector2)_TargetField.transform.position + (_TargetField.canvas.GetComponent<CanvasScaler>().referenceResolution/2);
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 mousePos = (Vector2)Input.mousePosition - new Vector2();
            
            if (Input.GetMouseButtonDown(0) && _TargetField.Raycast(mousePos, Camera.main))
            {
                Debug.Log("Target pos: " + _TargetArea.position);
                Debug.Log("Mouse pos: " + mousePos);
                Vector2 worldOffset = mousePos - _TargetArea.position;
                Debug.Log("worldOffset: " + worldOffset);
                Vector2 worldTargetSize = _TargetArea.size;
                Debug.Log("worldTargetSize: " + worldTargetSize);
                Vector2 normalizedTargetPosition = (worldOffset / worldTargetSize);
                Debug.Log("normalizedTargetPosition: " + normalizedTargetPosition);
                _WavePropagation.SpawnWave(normalizedTargetPosition);
            }
        }
    }
}
