using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class UIManager : MonoBehaviour
    {

        public void DestroyAllWaveInteractables()
        {
            var allObjects = FindObjectsOfType<WaveInteractable>();
            foreach(WaveInteractable wave in allObjects)
            {
                Destroy(wave.gameObject);
            }
        }
    }
}
