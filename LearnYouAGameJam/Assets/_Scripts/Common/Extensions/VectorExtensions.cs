using System.Diagnostics;
using UnityEngine;

namespace LYGJ.Common {
    public static class VectorExtensions {
        public static void Deconstruct( this Vector4 V, out float X, out float Y, out float Z, out float W ) {
            X = V.x;
            Y = V.y;
            Z = V.z;
            W = V.w;
        }
        public static void Deconstruct( this Vector3 V, out float X, out float Y, out float Z ) {
            X = V.x;
            Y = V.y;
            Z = V.z;
        }
        public static void Deconstruct( this Vector3Int V, out int X, out int Y, out int Z ) {
            X = V.x;
            Y = V.y;
            Z = V.z;
        }
        public static void Deconstruct( this Vector2 V, out float X, out float Y ) {
            X = V.x;
            Y = V.y;
        }
        public static void Deconstruct( this Vector2Int V, out int X, out int Y ) {
            X = V.x;
            Y = V.y;
        }
        
        public static Vector2Int AsVector2Int( this Vector2 V ) => new(Mathf.RoundToInt(V.x), Mathf.RoundToInt(V.y));

        public static float Random( this Vector2    V ) => UnityEngine.Random.Range(V.x, V.y);
        public static int   Random( this Vector2Int V ) => UnityEngine.Random.Range(V.x, V.y);
    }
}
