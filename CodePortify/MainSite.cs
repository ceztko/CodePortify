// Copyright (c) 2013 Francesco Pretto
// This file is subject to the MS-PL license

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using ClsPackage = Microsoft.VisualStudio.Shell.Package;

namespace CodePortify
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.GuidPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)] // Expose menus
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)] // Load if no solution
    sealed partial class MainSite : Package
    {
        private static MainSite _Package;
        private static DTE2 _DTE2;
        private static IVsMonitorSelection _MonitorSelection;
        private static IVsEditorAdaptersFactoryService _EditorAdaptersFactory;
        private static IVsSolution _Solution;

        public MainSite()
        {
            _Package = this;
        }

        protected override void Initialize()
        {
            base.Initialize();

            IVsExtensibility extensibility = GetService<IVsExtensibility>();
            _DTE2 = (DTE2)extensibility.GetGlobalsObject(null).DTE;
            _MonitorSelection = GetService<SVsShellMonitorSelection>() as IVsMonitorSelection;
            _EditorAdaptersFactory = GetComponentModelService<IVsEditorAdaptersFactoryService>();

            configurePackageSettings();
            subscribeEvents();
            initializeMenus();
        }

        public void GetService<T>(out T service)
        {
            service = (T)GetService(typeof(T));
        }

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public static void GetGlobalService<T>(out T service)
        {
            service = (T)ClsPackage.GetGlobalService(typeof(T));
        }

        public static T GetGlobalService<T>()
        {
            return (T)ClsPackage.GetGlobalService(typeof(T));
        }

        /// <summary>
        /// Get MEF components
        /// </summary>
        /// <returns>The MEF component of type T</returns>
        public T GetComponentModelService<T>()
            where T : class
        {
            IComponentModel componentModel = GetService<SComponentModel>() as IComponentModel;
            return componentModel.GetService<T>();
        }

        /// <summary>
        /// Get MEF components
        /// </summary>
        /// <param name="service">The MEF component of type T</param>
        public void GetComponentModelService<T>(out T service)
            where T : class
        {
            IComponentModel componentModel = GetService<SComponentModel>() as IComponentModel;
            service = componentModel.GetService<T>();
        }

        public static MainSite Package
        {
            get { return _Package; }
        }

        public static DTE2 DTE2
        {
            get { return _DTE2; }
        }

        public static IVsSolution Solution
        {
            get { return _Solution; }
        }

        public static IVsMonitorSelection MonitorSelection
        {
            get { return MainSite._MonitorSelection; }
        }

        public static IVsEditorAdaptersFactoryService EditorAdaptersFactory
        {
            get { return _EditorAdaptersFactory; }
        }
    }
}
