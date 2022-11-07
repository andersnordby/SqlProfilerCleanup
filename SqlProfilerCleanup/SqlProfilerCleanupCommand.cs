﻿using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Task = System.Threading.Tasks.Task;

// Starting point: 
// https://www.codeproject.com/Articles/1377559/How-to-Create-SQL-Server-Management-Studio-18-SSMS
// Note the comment about downgrading SDK to v15.0

// Parser:
// https://github.com/mgroves/SqlProfilerQueryCleaner

// Various useful code:
// https://github.com/devvcat/ssms-executor


namespace SqlProfilerCleanup
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SqlProfilerCleanupCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("941676d7-918c-4b4f-b56a-9410ed6ab092");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE2 dte;


        /// <summary>
        /// Initializes a new instance of the <see cref="SqlProfilerCleanupCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SqlProfilerCleanupCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.dte = (DTE2)(ServiceProvider.GetServiceAsync(typeof(DTE)).Result);

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SqlProfilerCleanupCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SqlProfilerCleanupCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SqlProfilerCleanupCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const string title = "SQL Profiler Cleanup";
            if (!dte.HasActiveDocument())
            {
                ShowError(title, "No active document found!");
                return;
            }

            var document = dte.GetDocument();
            var text = GetDocumentText(document);

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowError(title, "Document contains no text!");
                return;
            }

            var cleanedText = Cleaner.Clean(text);
            SetDocumentText(document, cleanedText);
        }

        private void ShowError(string title, string message)
        {
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static string GetDocumentText(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var content = string.Empty;
            
            if (document.Object("TextDocument") is TextDocument doc)
                content = doc.StartPoint.CreateEditPoint().GetText(doc.EndPoint);

            return content;
        }

        private static void SetDocumentText(Document document, string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(document.Object("TextDocument") is TextDocument doc)) 
                return;

            doc.StartPoint.CreateEditPoint().Delete(doc.EndPoint);
            doc.StartPoint.CreateEditPoint().Insert(text);
        }
    }
}