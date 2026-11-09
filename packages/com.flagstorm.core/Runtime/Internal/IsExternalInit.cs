namespace System.Runtime.CompilerServices
{
    // netstandard2.1 y el C# de Unity no traen este tipo; sin él no compilan los records ni `init`.
    internal static class IsExternalInit
    {
    }
}
