using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using S1API;

namespace ManorMod
{
    public static class MiscUtilities
    {
        
    }

    public static class CollectionUtilities
    {
        public static bool IsNotEmpty<T>(this List<T> list) => list.DefaultIfEmpty() != null;
        
        public static bool IsNotEmpty<T>(this T[] array) => array.DefaultIfEmpty() != null;

        public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable) => enumerable.DefaultIfEmpty() != null;
    }
}
