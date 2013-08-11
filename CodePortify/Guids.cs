using System;

namespace CodePortify
{
    static class GuidList
    {
        public const string GuidPackageString = "5d902955-59be-44cf-9339-f090b285b0e6";
        public const string GuidCmdSetString = "176c412f-2af9-4e2d-9612-ddd60bb726c1";

        public static readonly Guid GuidCmdSet = new Guid(GuidCmdSetString);
        public static readonly Guid GuidPackage = new Guid(GuidPackageString);
    };

    static class CmdIdList
    {
        public const uint ResetSettings = 0x100;
        public const uint IgnoreProject = 0x101;
    };
}