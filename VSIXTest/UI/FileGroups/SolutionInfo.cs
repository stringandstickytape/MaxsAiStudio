using System;
using System.IO;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

public class SolutionInfo
{
    public static string GetCurrentSolutionPath(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            if (dte?.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
            {
                return dte.Solution.FullName;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting solution path: {ex.Message}");
            return string.Empty;
        }
    }

    public static string GetSolutionDirectory(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string solutionPath = GetCurrentSolutionPath(dte);
        return !string.IsNullOrEmpty(solutionPath)
            ? Path.GetDirectoryName(solutionPath)
            : string.Empty;
    }

    public static string GetSolutionName(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string solutionPath = GetCurrentSolutionPath(dte);
        return !string.IsNullOrEmpty(solutionPath)
            ? Path.GetFileNameWithoutExtension(solutionPath)
            : string.Empty;
    }
}

