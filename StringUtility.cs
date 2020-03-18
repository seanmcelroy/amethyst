using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public static class StringUtility
{
    public static bool IsTypescriptType(this string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        if (string.Compare(typeName, "boolean") == 0)
            return true;

        if (string.Compare(typeName, "number") == 0)
            return true;

        if (string.Compare(typeName, "string") == 0)
            return true;

        if (string.Compare(typeName, "any") == 0)
            return true;

        if (string.Compare(typeName, "void") == 0)
            return true;

        if (string.Compare(typeName, "null") == 0)
            return true;

        if (string.Compare(typeName, "undefined") == 0)
            return true;

        if (string.Compare(typeName, "never") == 0)
            return true;

        if (string.Compare(typeName, "object") == 0)
            return true;

        // Array 1
        if (typeName.EndsWith("[]") && IsTypescriptType(typeName.Substring(0, typeName.Length - 2)))
            return true;

        // Array 2
        if (typeName.StartsWith("Array<") && typeName.EndsWith('>') && IsTypescriptType(typeName.Substring(6, typeName.Length - 1)))
            return true;

        // Tuple
        if (typeName.StartsWith('[') && typeName.EndsWith(']'))
            return true;

        return false;
    }

    public static string ToTitleCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
        else return text[0].ToString().ToUpperInvariant() + text.Substring(1);
    }

    public static IEnumerable<Tuple<string, string>> ReadManifestFiles<TSource>(string embeddedPrefix) where TSource : class
    {
        var assembly = typeof(TSource).GetTypeInfo().Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames().Where(s => s.StartsWith(embeddedPrefix, StringComparison.CurrentCultureIgnoreCase)))
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Could not load manifest resource stream for {resourceName}");
                }
                using (var reader = new StreamReader(stream))
                {
                    yield return new Tuple<string, string>(resourceName, reader.ReadToEnd());
                }
            }
    }

    public static string ReadManifestData<TSource>(string embeddedFileName) where TSource : class
    {
        var assembly = typeof(TSource).GetTypeInfo().Assembly;
        var resourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("Could not load manifest resource stream.");
            }
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}