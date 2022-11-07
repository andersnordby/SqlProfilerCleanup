using EnvDTE80;

namespace SqlProfilerCleanup
{
    // Copied from this project:
    // https://github.com/devvcat/ssms-executor
    public static class Extensions
    {
        public static bool HasActiveDocument(this DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var doc = dte?.ActiveDocument?.DTE?.ActiveDocument;
            return doc != null;
        }

        public static EnvDTE.Document GetDocument(this DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return dte.HasActiveDocument() 
                ? dte.ActiveDocument.DTE?.ActiveDocument 
                : null;
        }
    }
}