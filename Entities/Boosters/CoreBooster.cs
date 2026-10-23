// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Graphics.Animation;
using Celeste.Mods.JackalHelper.Modules;

namespace Celeste.Mods.JackalHelper.Entities.Boosters
{
    public class CoreBooster : OriginalBoosterState
    {
        public bool HasLaunched;
        public bool CanChange;
        public Sprite CoreSprite => BoosterSprite;

        public CoreBooster(ISourceEngineContext engine, Vector2 position, bool red, bool canChange) : base(engine,
            position, red)
        {
            CanChange = canChange;
            JackalHelperModule module = null;
            foreach (var candidate in ((ISourceEverestModulesEnvironment)engine).Modules)
                if (candidate is JackalHelperModule found)
                {
                    module = found;
                    break;
                }

            if (module == null) throw new InvalidOperationException("The owning JackalHelper module is not loaded.");
            Remove(BoosterSprite);
            BoosterSprite = module.CreateCoreBoosterSprite(red);
            Add(BoosterSprite);
            BoosterBloom.Alpha = .5f;
            BoosterParticleType.Color = red ? PackedColor.Maroon : PackedColor.Teal;
            BoosterParticleType.Color2 = red ? PackedColor.Maroon : PackedColor.Teal;
        }
    }
}
