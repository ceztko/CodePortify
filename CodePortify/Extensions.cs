// Copyright (c) 2013-2014 Francesco Pretto
// This file is subject to the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;

namespace CodePortify
{
    public static partial class Extensions
    {
        /*
        VCProject vsproject = (VCProject)project.Object;

        VCConfiguration vcconfiguration = (VCConfiguration)(((IVCCollection)vsproject.Configurations).Item("Debug"));
        VCUserMacro usermacro = (VCUserMacro)((IVCCollection)vcconfiguration.Tools).Item("UserMacro");
        usermacro*/

        public static Guid GetProjectGuid(this IVsHierarchy project)
        {
            Guid ret;
            project.GetGuidProperty(VSConstants.VSITEMID_ROOT,
                    (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out ret);
            return ret;
        }

        public static IVsHierarchy GetHierarchy(this IVsSolution solution)
        {
            return solution as IVsHierarchy;
        }

        public static IVsBuildMacroInfo GetBuildMacroInfo(this IVsProject project)
        {
            return project as IVsBuildMacroInfo;
        }

        public static IVsBuildPropertyStorage GetBuildPropertyStorage(this IVsHierarchy project)
        {
            return project as IVsBuildPropertyStorage;
        }

        public static ITextDocument GetTextDocument(this ITextBuffer textBuffer)
        {
            ITextDocument textDocument;
            bool success = textBuffer.Properties.TryGetProperty<ITextDocument>(
              typeof(ITextDocument), out textDocument);
            if (success)
                return textDocument;
            else
                return null;
        }

        public static IVsHierarchy GetProjectHierarchy(IVsSolution solution, Project project)
        {
            IVsHierarchy hierarchy;
            solution.GetProjectOfUniqueName(project.FullName, out hierarchy);
            return hierarchy;
        }

        public static IVsWindowFrame GetWindowFrame(this IVsTextView textView)
        {
            IVsTextViewEx textViewEx = textView as IVsTextViewEx;

            object ret;
            textViewEx.GetWindowFrame(out ret);
            return (IVsWindowFrame)ret;
        }

        public static HiearchyItemPair GetHiearchyItemPair(this IVsWindowFrame frame)
        {
            object hiearchy;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_Hierarchy, out hiearchy);

            object itemid;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_ItemID, out itemid);

            return new HiearchyItemPair((IVsHierarchy)hiearchy, (uint)(int)itemid);
        }

        public static ProjectItem GetProjectItem(this HiearchyItemPair pair)
        {
            object item;
            pair.Hierarchy.GetProperty(pair.ItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out item);
            return item as ProjectItem;
        }

        public static Project GetProject(this IVsHierarchy hierarchy)
        {
            object project;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project);
            return project as Project;
        }

        public static IEnumerable<Project> AllProjects(this Projects projects)
        {
            foreach (Project project in projects)
            {
                foreach (Project subproject in AllProjects(project))
                    yield return subproject;
            }
        }

        private static IEnumerable<Project> AllProjects(this Project project)
        {
            // if (proj.ConfigurationManager != null)
            // true project

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                // The Project is a Solution folder
                foreach (ProjectItem projectItem in project.ProjectItems)
                {
                    if (projectItem.SubProject != null)
                    {
                        // The ProjectItem is actually a Project
                        foreach (Project subproject in AllProjects(projectItem.SubProject))
                            yield return subproject;
                    }
                }
            }
            else
                yield return project;
        }
    }

    public class HiearchyItemPair
    {
        private IVsHierarchy _Hierarchy;
        private uint _ItemId;

        public HiearchyItemPair(IVsHierarchy hierarchy, uint itemid)
        {
            _Hierarchy = hierarchy;
            _ItemId = itemid;
        }

        public IVsHierarchy Hierarchy
        {
            get { return _Hierarchy; }
        }

        public uint ItemId
        {
            get { return _ItemId; }
        }
    }
}
