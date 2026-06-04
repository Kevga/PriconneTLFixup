using HarmonyLib;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using Type = Il2CppSystem.Type;

namespace PriconneTLFixup.Patches;

[HarmonyPatch]
internal class UnstrippingHooks
{
    [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string),
        typeof(Type))]
    [HarmonyPrefix]
    private static bool FixFontLoadPatch(AssetBundle __instance, ref Object __result, string name, Type type)
    {
        __result = FixFontLoad(__instance, name, type);
        return false;
    }
    
    public static Object FixFontLoad(AssetBundle __instance, string name, Type type)
    {
        IntPtr gcHandlePtr = default(IntPtr);
        Object result;
        try
        {
            unsafe
            {
                IntPtr intPtr = Object.MarshalledUnityObject.MarshalNotNull(__instance);
                if (intPtr == (IntPtr)0)
                {
                    ThrowHelper.ThrowNullReferenceException(__instance);
                }

                ManagedSpanWrapper managedSpanWrapper =
                    default(ManagedSpanWrapper);
                if (!StringMarshaller.TryMarshalEmptyOrNullString(name, ref managedSpanWrapper))
                {
                    ReadOnlySpan<char> readOnlySpan = name.AsSpan();
                    fixed (char* begin = readOnlySpan)
                    {
                        managedSpanWrapper = new ManagedSpanWrapper(begin, readOnlySpan.Length);
                        gcHandlePtr = AssetBundle.LoadAsset_Internal_Injected(intPtr, ref managedSpanWrapper, type);
                    }
                }
                else
                {
                    gcHandlePtr = AssetBundle.LoadAsset_Internal_Injected(intPtr, ref managedSpanWrapper, type);
                }
            }
        }
        finally
        {
            result = Unmarshal.UnmarshalUnityObject<Object>(gcHandlePtr);
        }

        return result;
    }
}