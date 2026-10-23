// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.JackalHelper.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.JackalHelper.Triggers.Movement
{
    [CustomEntity("JackalHelper/StaminaLockTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.JackalHelper.Code.StaminaLockTrigger")]
    public sealed class StaminaLockTrigger : SourceTriggerEntityState
    {
        private readonly JackalHelperModule module;

        public StaminaLockTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<JackalHelperModule>().Single();
            module.Session.inStaminaZone = false;
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            module.Session.inStaminaZone = true;
            base.OnEnter(player);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            module.Session.inStaminaZone = false;
            base.OnLeave(player);
        }
    }
}
