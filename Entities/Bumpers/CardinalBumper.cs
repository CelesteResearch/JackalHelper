// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.JackalHelper.Modules;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Input.Gameplay.Feedback;

namespace Celeste.Mods.JackalHelper.Entities.Bumpers
{
    [CustomEntity(
        "JackalHelper/CardinalBumper",
        "JackalHelper/LinkedCardinalBumper = LoadLinked")]
    [SceneTracked(false)]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.JackalHelper.Entities.CardinalBumper")]
    public class CardinalBumper : SceneEntityState, ISourceEngineContextEntity
    {
        public static ParticleTypePresentation P_Ambience;
        public static ParticleTypePresentation P_Launch;
        public static ParticleTypePresentation P_FireAmbience;
        public static ParticleTypePresentation P_FireHit;

        private readonly ISourceEngineContext engine;
        public ISourceEngineContext EngineContext => engine;

        public static CardinalBumper Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public bool Linked = false;

        public bool alwaysBumperBoost;
        public bool wobble;

        public Vector2 startPos;
        public Vector2 goal = Vector2.Zero;

        public int index;
        private Vector2[] positionNodes;

        public bool travelling = true;

        private float respawnTimer;

        // Graphics
        public Sprite[] outlines;
        private Sprite sprite;

        private VertexLightState light;

        private BloomPointState bloom;

        public static CardinalBumper LoadLinked(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData data)
        {
            return new CardinalBumper(level.EngineContext, data, offset) { Linked = true };
        }

        public CardinalBumper(ISourceEngineContext engine, Vector2 position, Vector2[] nodes, bool alwaysBumperBoost,
            bool wobble, string spriteDirectory, string overrideStr)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.alwaysBumperBoost = alwaysBumperBoost;
            this.wobble = wobble;
            startPos = Position;

            Collider = new HitboxColliderGeometry(20f, 20f, -10f, -10f);
            ColliderGeometry leftCollider = new HitboxColliderGeometry(1f, 16f, -11f, -8f);
            ColliderGeometry rightCollider = new HitboxColliderGeometry(1f, 16f, 10f, -8f);
            ColliderGeometry topCollider = new HitboxColliderGeometry(16f, 1f, -8f, -11f);
            ColliderGeometry bottomCollider = new HitboxColliderGeometry(16f, 1f, -8f, 10f);

            Add(new PlayerColliderComponentState(OnPlayerLeft, leftCollider));
            Add(new PlayerColliderComponentState(OnPlayerRight, rightCollider));
            Add(new PlayerColliderComponentState(OnPlayerTop, topCollider));
            Add(new PlayerColliderComponentState(OnPlayerBottom, bottomCollider));

            positionNodes = nodes;

            outlines = new Sprite[positionNodes.Length];
            for (int i = 0; i < positionNodes.Length; i++)
            {
                outlines[i] = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                    "objects/" + spriteDirectory + "/outline");
                outlines[i].Position = nodes[i];
                outlines[i].Visible = false;
            }

            goal = positionNodes[0];
            index = 0;

            Add(sprite = JackalHelperModule.CreateSprite(engine, spriteDirectory));
            Add(light = new VertexLightState(engine, PackedColor.Teal, 1f, 16, 32));
            Add(new VertexLightState(engine, PackedColor.Orange, 1f, 16, 32));
            Add(bloom = new BloomPointState(0.5f, 16f));
            Add(outlines);
            if (goal != Vector2.Zero)
            {
                UpdatePosition(goal);
            }
        }

        public CardinalBumper(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.NodesWithPosition(offset),
                data.Bool("alwaysBumperBoost", defaultValue: false), data.Bool("wobble", defaultValue: false),
                data.Attr("spriteDirectory", defaultValue: "bumperCardinal"),
                data.Attr("overrideString", defaultValue: ""))
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            sprite.Visible = true;
        }

        private void UpdatePosition(Vector2 position)
        {
            // COLOURSOFNOISE: You really need to use engine.DeltaTime for any movement/update stuff
            Vector2 path = position - Position;
            if (Math.Abs(path.X) < 0.5f && Math.Abs(path.Y) < 0.5f)
                // Snap alignment to the grid when resting
                Position = position;
            else
                Position += path / 0.5f * engine.DeltaTime;
        }

        public override void Update()
        {
            base.Update();
            Collidable = Vector2.Distance(Position, positionNodes[index]) < 24;
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    light.Visible = true;
                    bloom.Visible = true;
                    sprite.Play("on");

                    ((IOriginalGameplayProcess)engine).AudioSession.Play(
                        "event:/game/06_reflection/pinballbumper_reset", Position,
                        ((IOriginalGameplayProcess)engine).SaveContext);
                }
            }
            else if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.05f, 0f, engine))
            {
                ParticleTypePresentation type = (P_Ambience);
                if (type != null) // COLOURSOFNOISE: the particle type is never assigned...
                {
                    float direction = SourceRandomMath.NextAngle(((ISourceRandomEnvironment)engine).Random);
                    float length = (8);
                    ((OriginalLevelSceneRoot)Scene).Particles.Emit(type, 1,
                        Center + Vector2Geometry.AngleToVector(direction, length), Vector2.One * 2f, direction);
                }
            }

            if (goal == positionNodes[positionNodes.Length - 1])
            {
                Array.Reverse(positionNodes);
                index = 0;
                goal = positionNodes[index];
            }

            if (goal != Vector2.Zero)
            {
                UpdatePosition(goal);
            }
        }

        private void OnPlayer(RoomPlayerSession player, Vector2 launchVector)
        {
            if (respawnTimer <= 0f)
            {
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/06_reflection/pinballbumper_hit",
                    Position, ((IOriginalGameplayProcess)engine).SaveContext);
                respawnTimer = 0.7f;
                CardinalLaunch(player, launchVector);
                player.Machine.State = 0;
                sprite.Play("hit", restart: true);
                light.Visible = false;
                bloom.Visible = false;
                ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f);
            }
        }

        private void OnPlayerLeft(RoomPlayerSession player)
        {
            OnPlayer(player, new Vector2(-1f, -0.4f));
        }

        private void OnPlayerRight(RoomPlayerSession player)
        {
            OnPlayer(player, new Vector2(1f, -0.4f));
        }

        private void OnPlayerTop(RoomPlayerSession player)
        {
            OnPlayer(player, new Vector2(0f, -1f));
        }

        private void OnPlayerBottom(RoomPlayerSession player)
        {
            OnPlayer(player, new Vector2(0f, 1f));
        }

        public void CardinalLaunch(RoomPlayerSession player, Vector2 launchVector)
        {
            player.Data.Motion.Normal.VariableJumpTimer = 0f;

            player.Machine.State = 7;
            player.Data.Motion.AutoJump = true;

            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 1);
            ((ISourceFreezeEnvironment)engine).Freeze(0.1f);

            Vector2 speed = launchVector * 350f;
            if (!player.World.NoRefills)
            {
                player.World.RefillDash();
            }

            if (alwaysBumperBoost)
            {
                player.Data.Motion.Normal.Speed *= 1.4f;
            }

            OriginalSlashFxState.Burst(engine, Center, Vector2Geometry.Angle(speed));
            player.RefillStamina();
            player.Data.Motion.Normal.Speed = speed;

            if (Linked)
            {
                foreach (CardinalBumper bumper in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                             typeof(CardinalBumper)))
                {
                    if (bumper.Linked)
                    {
                        bumper.NextNode();
                    }
                }
            }
            else
            {
                NextNode();
            }
        }

        private void NextNode()
        {
            travelling = true;
            if (positionNodes.Length > 1)
            {
                if (Vector2.Distance(Position, positionNodes[index]) < 24f)
                {
                    index += 1;
                    goal = positionNodes[index];
                }
            }
        }
    }
}
