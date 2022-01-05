using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class SceneData : Singleton<SceneData>
    {
        bool _IsInitialised = false;

        Camera _CollisionCamera = null;

        void Initialize()
        {
            _CollisionCamera = GameObject.FindGameObjectWithTag("CollisionCamera").GetComponent<Camera>();
            _IsInitialised = true;
        }

        public Camera GetCollisionCamera()
        {
            if (!_IsInitialised) Initialize();

            return _CollisionCamera;
        }
    }
}
