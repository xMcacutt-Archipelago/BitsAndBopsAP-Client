using System;
using System.Collections.Generic;

namespace BitsAndBops_AP_Client;

public static class Extensions
{
    public static void ForEachItem<T>(this IEnumerable<T> container, Action<T> action)
    {
        foreach (T obj in container)
        {
            if (action != null)
                action(obj);
        }
    }
}