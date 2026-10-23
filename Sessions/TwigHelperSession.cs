// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using Celeste.Everest.Modules.Persistence;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.JackalHelper.Sessions
{
    [RuntimeTypeAlias("TwigHelperSession")]
    public sealed class TwigHelperSession : EverestModuleSession
    {
        public bool HasShroomDash { get; set; }
        public bool ShroomDashActive { get; set; }
        public bool ShroomDashTrailActive { get; set; }
    }
}
