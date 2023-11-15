using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace PriconneTLFixup;

public class CoroutineStarter : MonoBehaviour
{
    private static CoroutineStarter? _instance;
    public static Action? OnUpdate;
    public static Action? OnInit;

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
                OnInit?.Invoke();
            }

            var result = _instance;
            return result;
        }
    }
    
    public void Update()
    {
        OnUpdate?.Invoke();
    }
}