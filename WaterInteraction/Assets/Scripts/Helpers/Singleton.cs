using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    public class Singleton<T> where T : class, new()
    {
        #region Singleton
        protected static T _Instance;

        public static T Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new T();

                return _Instance;
            }
        }

        #endregion
    }
}
