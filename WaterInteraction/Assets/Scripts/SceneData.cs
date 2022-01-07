using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class SceneData : MonoBehaviour
    {
        #region Singleton
        private static SceneData _Instance;

        public static SceneData Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = GameObject.FindObjectOfType<SceneData>();
                }

                return _Instance;
            }
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        [SerializeField] SimulationData _SimData;
        public SimulationData SimData
        {
            get { 
                return _SimData; 
            }
        }

        NavierStokesPropagation _WavePropagation;
        public NavierStokesPropagation WavePropagation
        {
            get {
                if (_WavePropagation == null)
                    _WavePropagation = FindObjectOfType<NavierStokesPropagation>();
                return _WavePropagation; 
            }
        }

        CollisionBaker _CollisionBaker;
        public CollisionBaker CollisionBaker
        {
            get {
                if (_CollisionBaker == null)
                    _CollisionBaker = FindObjectOfType<CollisionBaker>(); 
                return _CollisionBaker; 
            }
        }

        CollisionRender _CollisionRender;
        public CollisionRender CollisionRender
        {
            get
            {
                if (_CollisionRender == null)
                    _CollisionRender = FindObjectOfType<CollisionRender>(); 
                return _CollisionRender; 
            }
        }
    }
}
