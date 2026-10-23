// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.JackalHelper.Triggers.Water
{
    // Source constructs this field but never adds it to the scene; it is not a map registration.
    public class KillBoxTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;

        public KillBoxTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (!((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible)
                player.Die(
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(player.Position - Position));
        }
    }
}
