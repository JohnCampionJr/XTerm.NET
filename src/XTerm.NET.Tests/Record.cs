namespace XTerm.Tests;

/// <summary>xUnit's Record.Exception, kept under its own name so call sites read unchanged.</summary>
internal static class Record
{
    public static Exception? Exception(Action action)
    {
        try { action(); return null; }
        catch (Exception e) { return e; }
    }
}
