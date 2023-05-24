using Il2CppSystem.Text;
using UnityEngine;

namespace PriconneTLFixup;

public static class Util
{
    public static string GetPath( this object obj )
    {
        StringBuilder path = new StringBuilder();
        var segments = GetPathSegments( obj );
        for( int i = 0; i < segments.Length; i++ )
        {
            path.Append( "/" ).Append( segments[ i ] );
        }

        return path.ToString();
    }
    
    public static string[] GetPathSegments( this object obj )
    {
        GameObject[] objects = new GameObject[ 128 ];
    
        var go = GetAssociatedGameObject( obj );
        if (go == null)
        {
            return Array.Empty<string>();
        }

        int i = 0;
        int j = 0;

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
}