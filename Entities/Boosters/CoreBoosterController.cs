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
    [CustomEntity("JackalHelper/RoundKevin")]
    public class CoreBoosterController : SceneEntityState, ISourceEngineContextEntity
    {
        public static CoreBoosterController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
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

        public bool controlCoreMode;


        public CoreBoosterController(ISourceEngineContext engine, Vector2 position, bool controlCoreMode)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.controlCoreMode = controlCoreMode;
        }

        public CoreBoosterController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position, data.Bool("controlCoreMode", defaultValue: true))
        {
        }

        public override void Awake(object scene)
        {
            OriginalLevelSceneRoot level = scene as OriginalLevelSceneRoot;
            if (level != null)
            {
                redo = false;
                if (firstLaunch)
                {
                    if (level.CoreMode == RoomCoreMode.None)
                    {
                        level.CoreMode = RoomCoreMode.Hot;
                    }

                    firstLaunch = false;
                }

                mode = level.CoreMode;
                red = (level.CoreMode == RoomCoreMode.Hot || level.CoreMode == RoomCoreMode.None);
                level.AddEntity(booster = new CoreBooster(engine, Position, red, controlCoreMode));
                booster.Position = Position + level.LevelOffset;

                booster.CoreSprite.Play(booster.CanChange ? "swap" : "swapSame");

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
                        .AddEntity(booster = new CoreBooster(engine, Position, red, controlCoreMode));

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
            if (booster.CanChange)
            {
                JackalHelperModule.GetCurrentLevel(engine).CoreMode = (red ? RoomCoreMode.Cold : RoomCoreMode.Hot);
            }

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
