using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodePortify
{
    sealed partial class MainSite
    {
        public const int CODE_PORTIFY_VER = 1;
        private const string PORTIFY_VERSION_VAR = "PortifyVersion";
        private const string IGNORE_SOLUTION_VAR = "IgnoreSolution";

        private static object _singletonLock = new object();
        private static WritableSettingsStore _SettingsStore;
        private static IVsExtensionManager _ExtensionManager;
        private static NormalizationSettingsCache _Singleton;

        protected override void OnLoadOptions(string key, Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            switch (key)
            {
                case PORTIFY_VERSION_VAR:
                    break;
                case IGNORE_SOLUTION_VAR:
                {
                    IVsSolution solution = GetService<SVsSolution>() as IVsSolution;

                    bool ignore = reader.ReadToEnd() == bool.TrueString;
                    if (ignore)
                        MainSite.SettingsCache.Ignore(solution.GetHierarchy());

                    _Solution = solution;
                    break;
                }
            }
            string read = reader.ReadToEnd();
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            switch (key)
            {
                case PORTIFY_VERSION_VAR:
                    writer.Write(CODE_PORTIFY_VER);
                    break;
                case IGNORE_SOLUTION_VAR:
                    bool ignoreSolution = MainSite.SettingsCache.IsIgnored(_Solution.GetHierarchy());
                    writer.Write(ignoreSolution);
                    break;
            }
            writer.Flush();
        }

        private void configurePackageSettings()
        {
            ShellSettingsManager manager = new ShellSettingsManager(this);
            _SettingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);

            createSettingsPreamble();
            bool prevSettingsExists = _SettingsStore.PropertyExists("CodePortify", "PrevCheckForConsistentLineEndings")
                && _SettingsStore.PropertyExists("CodePortify", "PrevDetectUTF8WithoutSignature");

            if (!prevSettingsExists)
            {
                bool prevValue = (bool)_DTE2.Properties["Environment", "Documents"].Item("CheckForConsistentLineEndings").Value;
                _SettingsStore.SetBoolean("CodePortify", "PrevCheckForConsistentLineEndings", prevValue);

                prevValue = (bool)_DTE2.Properties["TextEditor", "General"].Item("DetectUTF8WithoutSignature").Value;
                _SettingsStore.SetBoolean("CodePortify", "PrevDetectUTF8WithoutSignature", prevValue);
            }

            // Line endings will be handled here
            _DTE2.Properties["Environment", "Documents"].Item("CheckForConsistentLineEndings").Value = false;
            _DTE2.Properties["TextEditor", "General"].Item("DetectUTF8WithoutSignature").Value = true;

            _ExtensionManager = GetService<SVsExtensionManager>() as IVsExtensionManager;
            _ExtensionManager.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_ExtensionManager_PropertyChanged);

            // Add solution user options handling
            AddOptionKey(PORTIFY_VERSION_VAR);
            AddOptionKey(IGNORE_SOLUTION_VAR);
        }

        void _ExtensionManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // CHECK-ME: This is a bad way to verify that the Extension has been uninstalled. A
            // better way is needed to perform some custom rollback/cleaning for the package
            IInstalledExtension extension;
            if (_ExtensionManager.RestartRequired == RestartReason.PendingUninstall
                && !_ExtensionManager.TryGetInstalledExtension("CodePortify", out extension)
                && _SettingsStore.CollectionExists("CodePortify"))
            {
                _DTE2.Properties["Environment", "Documents"].Item("CheckForConsistentLineEndings").Value =
                    _SettingsStore.GetBoolean("CodePortify", "PrevCheckForConsistentLineEndings", true);
                _DTE2.Properties["TextEditor", "General"].Item("DetectUTF8WithoutSignature").Value =
                    _SettingsStore.GetBoolean("CodePortify", "PrevDetectUTF8WithoutSignature", true);

                _SettingsStore.DeleteCollection("CodePortify");
            }
        }

        private void createSettingsPreamble()
        {
            _SettingsStore.CreateCollection("CodePortify");
            _SettingsStore.SetString("CodePortify", "PortifyVersion", MainSite.CODE_PORTIFY_VER.ToString());
        }

        public static WritableSettingsStore SettingsStore
        {
            get { return _SettingsStore; }
        }

        public static IVsExtensionManager ExtensionManager
        {
            get { return _ExtensionManager; }
        }

        public static NormalizationSettingsCache SettingsCache
        {
            get
            {
                // Double-checked locking semantics
                if (_Singleton == null)
                {
                    lock (_singletonLock)
                    {
                        if (_Singleton == null)
                            _Singleton = new NormalizationSettingsCache();
                    }
                }

                return _Singleton;
            }
        }
    }
}
