using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Collections;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodePortify
{
    public class NormalizationSettingsCache
    {
        private const string PORTIFY_IGNORE_VAR = "PortifyIgnore";
        private const string PORTIFY_VERSION_VAR = "PortifyVersion";
        private const string PORTIFY_PREFIX = "portify_";
        private const string IGNORE_EXT = "IGN";
        private const string TRUE_VALUE = "True";
        private const string FALSE_VALUE = "False";

        private object _lock;
        private Dictionary<IVsHierarchy, Dictionary<string, NormalizationSettings>> _settings;

        internal NormalizationSettingsCache()
        {
            _lock = new object();
            _settings = new Dictionary<IVsHierarchy, Dictionary<string, NormalizationSettings>>();
        }

        public NormalizationSettings this[TextViewIdentity identity]
        {
            get
            {
                lock (_lock)
                {
                    return getSettings(identity.Hierarchy, identity.Extension);
                }
            }
            set
            {
                lock (_lock)
                {
                    setSettings(identity.Hierarchy, identity.Extension, value);
                }
            }
        }

        public bool IsIgnored(IVsHierarchy hierarchy)
        {
            Dictionary<string, NormalizationSettings> inner;
            bool found = _settings.TryGetValue(hierarchy, out inner);

            if (found && inner == null)
                return true;

            return false;
        }

        public void Ignore(IVsHierarchy hierarchy)
        {
            lock (_lock)
            {
                ignore(hierarchy);
            }
        }

        public void Reset(IVsHierarchy hierarchy)
        {
            lock (_lock)
            {
                reset(hierarchy);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _settings.Clear();
            }
        }

        private void ignore(IVsHierarchy hierarchy)
        {
            reset(hierarchy);
            _settings[hierarchy] = null;

            if (hierarchy == MainSite.Solution)
            {
                // Solution user options are saved separately
                return;
            }

            // Save project user settings
            IVsBuildPropertyStorage storage = hierarchy.GetBuildPropertyStorage();
            writeSettingsPreamble(null, storage, true);
        }

        private void reset(IVsHierarchy hierarchy)
        {
            _settings.Remove(hierarchy);

            Globals globals = hierarchy.GetProject().Globals;
            if (hierarchy == MainSite.Solution)
            {
                cleanGlobalsSettings(globals);
            }
            else
            {
                IVsBuildPropertyStorage storage = hierarchy.GetBuildPropertyStorage();
                cleanSettings(globals, storage);
            }
        }

        private NormalizationSettings getSettings(IVsHierarchy hierarchy, string extension)
        {
            Dictionary<string, NormalizationSettings> inner;
            bool found = _settings.TryGetValue(hierarchy, out inner);

            try
            {
                return inner[extension];
            }
            catch
            {
                if (found && inner == null)
                {
                    // The hierarchy has been registered but it's ignored
                    goto ReturnIgnored;
                }

                Globals globals = hierarchy.GetProject().Globals;
                IVsBuildPropertyStorage storage = hierarchy.GetBuildPropertyStorage();

                string storageVerString;
                storage.GetPropertyValue(PORTIFY_VERSION_VAR, null, (int)_PersistStorageType.PST_USER_FILE,
                    out storageVerString);

                bool success;
                if (!string.IsNullOrEmpty(storageVerString))
                {
                    int storageSettingsVer;
                    success = int.TryParse(storageVerString, out storageSettingsVer);
                    if (!success || storageSettingsVer < MainSite.CODE_PORTIFY_VER)
                        goto CleanSettings;

                    string ignoreVariable;
                    storage.GetPropertyValue(PORTIFY_IGNORE_VAR, null, (int)_PersistStorageType.PST_USER_FILE,
                        out ignoreVariable);
                    if (ignoreVariable == TRUE_VALUE)
                        goto ReturnIgnored;
                }

                string globalsVerString = globals[PORTIFY_VERSION_VAR] as string;

                int globalsSettingsVer;
                success = int.TryParse(globalsVerString, out globalsSettingsVer);
                if (!success || globalsSettingsVer < MainSite.CODE_PORTIFY_VER)
                    goto CleanSettings;

                string settingsVariableName = PORTIFY_PREFIX + extension;
                string settingsString = globals[settingsVariableName] as string;
                if (settingsString == null)
                    goto ReturnNull;

                NormalizationSettings newSettings = null;
                try
                {
                    deserialize(settingsString);
                    _settings[hierarchy][extension] = newSettings;
                }
                catch
                {
                    globals.VariablePersists[settingsVariableName] = false;
                    globals[settingsVariableName] = null;
                    goto ReturnNull;
                }

                return newSettings;

            CleanSettings:
                cleanSettings(globals, storage);

            ReturnNull:
                _settings[hierarchy][extension] = null;
                return null;

            ReturnIgnored:
                NormalizationSettings ret = new NormalizationSettings();
                ret.Ignore = true;
                return ret;
            }
        }

        private void setSettings(IVsHierarchy hierarchy, string extension, NormalizationSettings value)
        {
            Globals globals = hierarchy.GetProject().Globals;

            writeSettingsPreamble(globals, null, false);
            globals.VariablePersists[PORTIFY_VERSION_VAR] = true;
            string variableName = PORTIFY_PREFIX + extension;
            globals[variableName] = serialize(value);
            globals.VariablePersists[variableName] = true;
        }

        private void writeSettingsPreamble(Globals globals, IVsBuildPropertyStorage storage, bool ignore)
        {
            if (ignore)
            {
                // This is a bit forcing the purpouse of IVsBuildPropertyStorage, that should
                // be used only for setting build properties, but it works
                storage.SetPropertyValue(PORTIFY_VERSION_VAR, null, (int)_PersistStorageType.PST_USER_FILE,
                    MainSite.CODE_PORTIFY_VER.ToString());
                storage.SetPropertyValue(PORTIFY_IGNORE_VAR, null, (int)_PersistStorageType.PST_USER_FILE,
                     ignore ? TRUE_VALUE : FALSE_VALUE);
            }
            else
            {
                globals[PORTIFY_VERSION_VAR] = MainSite.CODE_PORTIFY_VER.ToString();
            }
        }

        private void cleanSettings(Globals globals, IVsBuildPropertyStorage storage)
        {
            cleanGlobalsSettings(globals);
            cleanStorageSettings(storage);
        }

        private void cleanGlobalsSettings(Globals globals)
        {
            foreach (string variableName in globals.VariableNames as IEnumerable)
            {
                if (variableName == PORTIFY_VERSION_VAR
                    || variableName == PORTIFY_IGNORE_VAR
                    || variableName.StartsWith(PORTIFY_PREFIX))
                {
                    globals.VariablePersists[variableName] = false;
                    globals[variableName] = null;
                }
            }
        }

        private void cleanStorageSettings(IVsBuildPropertyStorage storage)
        {
            storage.RemoveProperty(PORTIFY_VERSION_VAR, null, (int)_PersistStorageType.PST_USER_FILE);
            storage.RemoveProperty(PORTIFY_IGNORE_VAR, null, (int)_PersistStorageType.PST_USER_FILE);
        }

        private string serialize(NormalizationSettings settings)
        {
            if (settings.Ignore)
                return IGNORE_EXT;

            StringBuilder builder = new StringBuilder();
            builder.Append(toString(settings.Operation));
            builder.Append(";");
            builder.Append(toString(settings.NewLineCharacter));
            builder.Append(";");
            builder.Append(settings.SaveUTF8);
            return builder.ToString();
        }

        private NormalizationSettings deserialize(string value)
        {
            NormalizationSettings ret = new NormalizationSettings();

            string[] splitted = value.Split(';');
            if (splitted[0] == IGNORE_EXT)
            {
                ret.Ignore = true;
                return ret;
            }

            ret.Operation = fromStringOperation(splitted[0]);
            ret.NewLineCharacter = fromStringCharacter(splitted[1]);
            ret.SaveUTF8 = bool.Parse(splitted[2]);

            return ret;
        }

        private string toString(NewLineCharacter character)
        {
            switch (character)
            {
                case NewLineCharacter.CRLF:
                    return "CRLF";
                case NewLineCharacter.LF:
                    return "LF";
                case NewLineCharacter.CR:
                    return "CR";
                default:
                    throw new Exception();
            }
        }

        private NewLineCharacter fromStringCharacter(string value)
        {
            switch (value)
            {
                case "CRLF":
                    return NewLineCharacter.CRLF;
                case "LF":
                    return NewLineCharacter.LF;
                case "CR":
                    return NewLineCharacter.CR;
                default:
                    throw new Exception();
            }
        }

        private string toString(TabifyOperation operation)
        {
            switch (operation)
            {
                case TabifyOperation.Tabify:
                    return "TAB";
                case TabifyOperation.Untabify:
                    return "UNTAB";
                default:
                    throw new Exception();
            }
        }

        private TabifyOperation fromStringOperation(string value)
        {
            switch (value)
            {
                case "TAB":
                    return TabifyOperation.Tabify;
                case "UNTAB":
                    return TabifyOperation.Untabify;
                default:
                    throw new Exception();
            }
        }
    }
}
