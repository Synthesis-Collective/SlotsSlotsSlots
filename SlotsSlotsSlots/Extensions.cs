using System;
using System.Collections.Generic;
using Mutagen.Bethesda;

namespace SlotsSlotsSlots
{
    public static class Extensions
    {
        public static IEnumerable<T> Do<T>(this IEnumerable<T> coll, Action<T> modify)
        {
            foreach (var itm in coll)
            {
                modify(itm);
                yield return itm;
            }
        }
        
    }
}