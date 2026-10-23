// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.JackalHelper.Entities.Water;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.JackalHelper.Modules
{
    public sealed partial class JackalHelperModule : SourceEverestModule
    {
        private IDisposable swimJumpHook;

        public JackalHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
            swimJumpHook = new Hook(
                typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.SwimJumpCheck)),
                (Func<Func<RoomPlayerWorld, bool>, RoomPlayerWorld, bool>)SwimJumpCheck);
            LoadStaminaLocks();
        }

        public override void Unload()
        {
            swimJumpHook?.Dispose();
            swimJumpHook = null;
            UnloadStaminaLocks();
        }

        private bool SwimJumpCheck(Func<RoomPlayerWorld, bool> original, RoomPlayerWorld world)
        {
            var player = world.SourcePlayer;
            if (player.Scene is ISourceEngineContextScene scene && ReferenceEquals(scene.EngineContext, SourceEngine))
            {
                var waters = ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities("Celeste.Water");
                SceneEntityState nearest = null;
                float distance = 0;
                foreach (SceneEntityState water in waters)
                {
                    float candidate =
                        ((ISourceVectorEnvironment)SourceEngine).VectorArithmetic.DistanceSquared(player.Position,
                            water.Position);
                    if (nearest == null || candidate < distance)
                    {
                        nearest = water;
                        distance = candidate;
                    }
                }

                if (nearest is DeadlyWater) return false;
            }

            return original(world);
        }
    }
}
