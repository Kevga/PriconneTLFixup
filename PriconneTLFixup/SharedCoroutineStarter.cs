using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Type = Il2CppSystem.Type;

namespace PriconneTLFixup;

public class CoroutineStarter : MonoBehaviour
{
    private static CoroutineStarter? _instance;

    public static CoroutineStarter Instance
    {
        get
        {
            if (_instance == null)
            {
                Log.Debug("Creating new CoroutineStarter");
                ClassInjector.RegisterTypeInIl2Cpp<CoroutineStarter>();
                var gameObject = new GameObject();
                _instance = gameObject.AddComponent<CoroutineStarter>();
                gameObject.name = typeof(CoroutineStarter).ToString();
                DontDestroyOnLoad(gameObject);
            }

            var result = _instance;
            return result;
        }
    }
}