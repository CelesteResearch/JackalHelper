// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Environment.Water;

namespace Celeste.Mods.JackalHelper.Entities.Water
{
    internal sealed class DeadlyWaterAppearance : IOriginalWaterStatics
    {
        private readonly IOriginalWaterStatics original;
        public PackedColor FillColor { get; }
        public PackedColor SurfaceColor { get; }
        public PackedColor RayTopColor { get; }
        public Vector2 RayAngle => original.RayAngle;

        public DeadlyWaterAppearance(ISourceEngineContext engine, PackedColor color)
        {
            original = ((IOriginalWaterStaticsEnvironment)engine).WaterStatics;
            SurfaceColor = color;
            FillColor = color * .2f;
            RayTopColor = color * .4f;
        }
    }
}
