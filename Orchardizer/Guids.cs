// Guids.cs
// MUST match guids.h

using System;

namespace Orchardizer
{
    static class GuidList
    {
        public const string guidThemeCreatorPkgString = "503bb8fb-9dbc-4217-9d45-f5f59ce613b5";
        public const string guidThemeCreatorCmdSetString = "ac46be5f-262e-4cb6-8733-2aa013a30171";

        public static readonly Guid guidThemeCreatorCmdSet = new Guid(guidThemeCreatorCmdSetString);
    };
}