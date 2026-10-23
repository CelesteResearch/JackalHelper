// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Environment.Water;

namespace Celeste.Mods.JackalHelper.Entities.Water
{
    internal sealed class DeadlyWaterSurface : OriginalWaterState.Surface
    {
        public DeadlyWaterSurface(ISourceEngineContext engine, Vector2 position, Vector2 outwards, float width,
            float height, IOriginalWaterStatics appearance) : base(engine, position, outwards, width, height,
            appearance)
        {
        }
    }
}
