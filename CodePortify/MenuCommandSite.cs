using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System.Globalization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace CodePortify
{
    sealed partial class MainSite
    {
        private void initializeMenus()
        {
            OleMenuCommandService menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            CommandID menuCommandID = new CommandID(GuidList.GuidCmdSet, (int)CmdIdList.ResetSettings);
            OleMenuCommand resetMenuItem = new OleMenuCommand(resetSettingsCallback, menuCommandID);
            resetMenuItem.BeforeQueryStatus += new EventHandler(menuItem_BeforeQueryStatus);
            menuCommandService.AddCommand(resetMenuItem);

            menuCommandID = new CommandID(GuidList.GuidCmdSet, (int)CmdIdList.IgnoreProject);
            OleMenuCommand ignoreMenuItem = new OleMenuCommand(ignoreProjectCallback, menuCommandID);
            ignoreMenuItem.BeforeQueryStatus += new EventHandler(menuItem_BeforeQueryStatus);
            menuCommandService.AddCommand(ignoreMenuItem);
        }

        private void resetSettingsCallback(object sender, EventArgs e)
        {
            int hr;
            IntPtr hierarchyPtr = IntPtr.Zero;
            uint itemid;
            IVsMultiItemSelect multiItemSelect;
            IntPtr selectionContainer = IntPtr.Zero;
            hr = _MonitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid,
                    out multiItemSelect, out selectionContainer);
            ErrorHandler.ThrowOnFailure(hr);

            // NB: Marshal.Relase() is not needed. The example in the How-to "Persist the
            // Property of a Project Item" is wrong
            IVsHierarchy hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;

            MainSite.SettingsCache.Reset(hierarchy);

            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                "Code Portify",
                "Selected project settings have been cleaned",
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));
        }

        private void ignoreProjectCallback(object sender, EventArgs e)
        {
            int hr;
            IntPtr hierarchyPtr = IntPtr.Zero;
            uint itemid;
            IVsMultiItemSelect multiItemSelect;
            IntPtr selectionContainer = IntPtr.Zero;
            hr = _MonitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid,
                    out multiItemSelect, out selectionContainer);
            ErrorHandler.ThrowOnFailure(hr);

            // NB: Marshal.Relase() is not needed. The example in the How-to "Persist the
            // Property of a Project Item" is wrong
            IVsHierarchy hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;

            MainSite.SettingsCache.Ignore(hierarchy);

            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                "Code Portify",
                "Selected project will be ignored",
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));
        }

        private void menuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (_DTE2.SelectedItems.Count == 1)
                menuCommand.Visible = true;
            else
                menuCommand.Visible = false;
        }
    }
}
