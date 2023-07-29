using Il2CppSystem.Text;
using UnityEngine;

namespace PriconneTLFixup;

public static class Util
{
    public static float GetTranslationDelayInSeconds()
    {
        return Plugin.AutoTranslatorPlugin.TranslationManager.CurrentEndpoint.TranslationDelay;
    }
    
    public static string GetPath( this object obj )
    {
        var path = new StringBuilder();
        var segments = GetPathSegments( obj );
        for( var i = 0; i < segments.Length; i++ )
        {
            path.Append( "/" ).Append( segments[ i ] );
        }

        return path.ToString();
    }
    
    public static string[] GetPathSegments( this object obj )
    {
        var objects = new GameObject[ 128 ];
    
        var go = GetAssociatedGameObject( obj );
        if (go == null)
        {
            return Array.Empty<string>();
        }

        var i = 0;
        var j = 0;

        objects[ i++ ] = go;
        while( go.transform.parent != null )
        {
            go = go.transform.parent.gameObject;
            objects[ i++ ] = go;
        }

        var result = new string[ i ];
        while( --i >= 0 )
        {
            result[ j++ ] = objects[ i ].name;
            objects[ i ] = null!;
        }

        return result;
    }
    
    private static GameObject GetAssociatedGameObject( object obj )
    {
        if( obj is GameObject go )
        {

        }
        else if( obj is Component comp )
        {
            try
            {
                go = comp.gameObject;
            }
            catch( Exception )
            {
                return null!;
            }
        }
        else
        {
            throw new ArgumentException( "Expected object to be a GameObject or component.", "obj" );
        }

        return go;
    }
    
    public class WaitForSecondsOrPredicate : CustomYieldInstruction
    {
        private readonly Func<bool> _predicate;
        private float _timeout;
        private bool KeepWaiting()
        {
            _timeout -= Time.deltaTime;
            return _timeout > 0f && !_predicate();
        }
 
        public override bool keepWaiting => KeepWaiting();

        public WaitForSecondsOrPredicate(float timeout, Func<bool> predicate)
        {
            _predicate = predicate;
            _timeout = timeout;
        }
    }
}