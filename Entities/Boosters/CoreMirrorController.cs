// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.JackalHelper.Modules;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;

namespace Celeste.Mods.JackalHelper.Entities.Boosters
{
    [CustomEntity("JackalHelper/RoundKevin2")]
    public class CoreMirrorController : SceneEntityState, ISourceEngineContextEntity
    {
        public static CoreMirrorController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public CoreBooster booster;

        public bool red;

        public object scene;

        public bool hasLaunched = false;

        public bool firstLaunch = true;

        public RoomCoreMode mode;

        private readonly ISourceEngineContext engine;

        public ISourceEngineContext EngineContext => engine;

        public Vector2 playerPosition;

        public bool redo;


        public CoreMirrorController(ISourceEngineContext engine, Vector2 position)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
        }

        public CoreMirrorController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position)
        {
        }

        public override void Awake(object scene)
        {
            if (JackalHelperModule.GetCurrentLevel(engine) != null)
            {
                redo = false;
                if (firstLaunch)
                {
                    if (JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.None)
                    {
                        JackalHelperModule.GetCurrentLevel(engine).CoreMode = RoomCoreMode.Hot;
                    }

                    firstLaunch = false;
                }

                mode = JackalHelperModule.GetCurrentLevel(engine).CoreMode;
                red = (JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.Hot ||
                       JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.None);
                JackalHelperModule.GetCurrentLevel(engine)
                    .AddEntity(booster = new CoreBooster(engine, Position, red, false));
                booster.Position = Position + JackalHelperModule.GetCurrentLevel(engine).LevelOffset;

                booster.CoreSprite.Play("swap");
                ((IOriginalGameplayProcess)engine).AudioSession.Play(
                    red
                        ? "event:/game/05_mirror_temple/redbooster_reappear"
                        : "event:/game/04_cliffside/greenbooster_reappear", Position,
                    ((IOriginalGameplayProcess)engine).SaveContext);
            }
            else
            {
                redo = true;
            }
        }


        public override void Removed(object scene)
        {
            base.Removed(scene);
            redo = true;
        }

        public override void Update()
        {
            if (redo)
            {
                if (JackalHelperModule.GetCurrentLevel(engine) != null)
                {
                    redo = false;
                    if (firstLaunch)
                    {
                        if (JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.None)
                        {
                            JackalHelperModule.GetCurrentLevel(engine).CoreMode = RoomCoreMode.Hot;
                        }

                        firstLaunch = false;
                    }

                    mode = JackalHelperModule.GetCurrentLevel(engine).CoreMode;
                    red = (JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.Hot ||
                           JackalHelperModule.GetCurrentLevel(engine).CoreMode == RoomCoreMode.None);
                    JackalHelperModule.GetCurrentLevel(engine)
                        .AddEntity(booster = new CoreBooster(engine, Position, red, false));

                    booster.CoreSprite.Play("swap");
                    ((IOriginalGameplayProcess)engine).AudioSession.Play(
                        red
                            ? "event:/game/05_mirror_temple/redbooster_reappear"
                            : "event:/game/04_cliffside/greenbooster_reappear", Position,
                        ((IOriginalGameplayProcess)engine).SaveContext);
                }
            }
            else
            {
                if (JackalHelperModule.GetCurrentPlayer(engine) != null)
                {
                    StateCheck(JackalHelperModule.GetCurrentPlayer(engine), booster);
                }

                if (JackalHelperModule.GetCurrentLevel(engine) != null)
                {
                    mode = JackalHelperModule.GetCurrentLevel(engine).CoreMode;
                }
            }
        }


        public void Destroy(CoreBooster booster)
        {
            //sprite.Visible = false;
            Remove(booster.CoreSprite);
            booster.RemoveSelf();
            JackalHelperModule.GetCurrentLevel(engine).CoreMode = (red ? RoomCoreMode.Cold : RoomCoreMode.Hot);
            Awake(Scene);
        }

        public void StateCheck(RoomPlayerSession player, CoreBooster booster)
        {
            if (player.LastBooster is CoreBooster || player.LastBooster == null)
            {
                if (player.Machine.State == 4)
                {
                    hasLaunched = true;

                    playerPosition = player.Position;
                }

                if ((player.Machine.State != 4 && player.Machine.State != 5) && hasLaunched &&
                    (((ISourceVectorEnvironment)engine).VectorArithmetic.Distance(player.Position,
                        player.LastBooster.Position) > 28f))
                {
                    hasLaunched = false;
                    Destroy(booster);
                }
                else if (mode != JackalHelperModule.GetCurrentLevel(engine).CoreMode)
                {
                    hasLaunched = false;
                    Destroy(booster);
                }
            }
        }
    }
}
