using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace CodePortify
{
    public sealed partial class MainSite
    {
        private SolutionEvents _SolutionEvents;
#if DEBUG
        private CommandEvents _CommandEvents;
#endif

        private void subscribeEvents()
        {
            _SolutionEvents = _DTE2.Events.SolutionEvents;
            _SolutionEvents.AfterClosing += new _dispSolutionEvents_AfterClosingEventHandler(_SolutionEvents_AfterClosing);

#if DEBUG
            _CommandEvents = _DTE2.Events.CommandEvents;
            _CommandEvents.BeforeExecute += new _dispCommandEvents_BeforeExecuteEventHandler(_CommandEvents_BeforeExecute);
#endif
        }

        void _SolutionEvents_AfterClosing()
        {
            // Clear settings cache after closing the solution
            MainSite.SettingsCache.Clear();
            _Solution = null;
        }

        void _CommandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            //if (Guid == "{5EFC7975-14BC-11CF-9B2B-00AA00573819}" && ID == 264)
            //    System.Diagnostics.Debugger.Break();
        }
    }
}
