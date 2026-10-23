// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Persistence;

namespace Celeste.Mods.JackalHelper.Sessions
{
    [RuntimeTypeAlias("Celeste.Mod.JackalHelper.JackalHelperSession")]
    public sealed partial class JackalHelperSession : EverestModuleSession
    {
        public bool inStaminaZone { get; set; }
    }
}
