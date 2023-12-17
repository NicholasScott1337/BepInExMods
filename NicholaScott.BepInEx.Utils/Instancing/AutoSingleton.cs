using UnityEngine;

namespace NicholaScott.BepInEx.Utils.Instancing
{
    public class AutoSingleton<TPrepare> where TPrepare : UnityEngine.Object
    {
        private static TPrepare _instance;
        public static TPrepare Instance
        {
            get
            {
                if (!_instance)
                    _instance = Object.FindObjectOfType<TPrepare>();
                return _instance;
            }
        }
    }
}