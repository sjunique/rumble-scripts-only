using UnityEngine;

 
  public static class TransformPathExt
{
    public static string GetHierarchyPath(this Transform t)
    {
        System.Collections.Generic.List<string> parts = new();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }
}

 
