using System.IO;

public static class DirectoryUtils
{
    public static string ReplaceBackWithForwardSlashes(string s)
    {
        return s.Replace("\\", "/");
    }

    public static void EnsureFolderExists(string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }
}