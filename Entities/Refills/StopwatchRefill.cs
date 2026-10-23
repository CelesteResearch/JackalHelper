// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Mods.JackalHelper.Modules;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.JackalHelper.Entities.Refills
{
    [CustomEntity("JackalHelper/TracerRefill")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.JackalHelper.Entities.StopwatchRefill")]
    public class StopwatchRefill : SceneEntityState
    {
        private readonly IOriginalRefillProcess engine;

        // COLOURSOFNOISE: None of these particle types are used
        public static ParticleTypePresentation P_Shatter;
        public static ParticleTypePresentation P_Regen;
        public static ParticleTypePresentation P_Glow;

        public static ParticleTypePresentation P_ShatterTwo;
        public static ParticleTypePresentation P_RegenTwo;
        public static ParticleTypePresentation P_GlowTwo;

        private ParticleTypePresentation p_shatter;
        private ParticleTypePresentation p_regen;
        private ParticleTypePresentation p_glow;

        private Sprite sprite;
        private Sprite flash;
        private SpriteImage outline;

        private WigglerComponent wiggler;
        private SineWaveState sine;

        private BloomPointState bloom;
        private VertexLightState light;

        private IOriginalRefillScene level;

        private bool oneUse;
        private bool refillDash;

        public float recallTime;

        // COLOURSOFNOISE: This needs to be implemented in Ahorn
        private bool storeFollowers;

        private float respawnTimer;

        public bool timed;

        public float recallTimer = 0f;

        private bool resetState = false;

        public StopwatchRefill(Vector2 position, bool oneUse, bool refillDash, float time, bool storeFollowers,
            bool resetState, IOriginalRefillProcess engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.refillDash = refillDash;
            recallTime = time;
            this.oneUse = oneUse;
            this.storeFollowers = storeFollowers;

            Depth = -100;
            Collider = new HitboxColliderGeometry(16f, 16f, -8f, -8f);

            Add(new PlayerColliderComponentState(OnPlayer));

            p_shatter = engine.RefillParticles.P_Shatter;
            p_regen = engine.RefillParticles.P_Regen;
            p_glow = engine.RefillParticles.P_Glow;

            string str = "objects/stopwatch/";
            Add(outline = new SpriteImage(engine.GameGraphics.GameAtlas[str + "outline"]));
            outline.CenterOrigin();
            outline.Visible = false;
            Add(sprite = new Sprite(engine, engine.GameGraphics.GameAtlas, str + "idle"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            Add(flash = new Sprite(engine, engine.GameGraphics.GameAtlas, str + "flash"));
            flash.Add("flash", "", 0.05f);
            flash.OnFinish = delegate { flash.Visible = false; };
            flash.CenterOrigin();

            Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 4f,
                delegate(float v) { sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f)); }));
            Add(sine = new SineWaveState(engine, 0.6f).Randomize());

            Add(new MirrorReflectionState());

            Add(bloom = new BloomPointState(0.8f, 16f));
            Add(light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, 1f, 16, 48));
            this.resetState = resetState;
        }

        public StopwatchRefill(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("oneUse"), data.Bool("RefillDashOnUse", defaultValue: true),
                data.Float("time"), data.Bool("storeFollowers"), data.Bool("resetState", defaultValue: false),
                (IOriginalRefillProcess)engine)
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = Scene as IOriginalRefillScene;
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }

            if (recallTimer > 0f && recallTimer < recallTime)
            {
                recallTimer += engine.DeltaTime;
                timed = true;
            }
            else if (recallTimer >= recallTime)
            {
                if ((JackalHelperModule.GetCurrentPlayer(engine) is { } player))
                {
                    if (storeFollowers)
                        MoveFollowers(player.Leader, (Position + new Vector2(0f, 8f)) - player.Position);

                    player.Position = (Position + new Vector2(0f, 8f));
                    //Optional: reset state
                    if (resetState)
                        player.Machine.State = 0;
                    //player.Machine.State = 0;
                }

                timed = false;
                recallTimer = 0f;
            }


            /*
            else if (base.Scene.OnInterval(0.1f))
            {
                level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
            }*/
            UpdateY();
            light.Alpha = ScalarMath.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * engine.DeltaTime,
                engine.ExtremaArithmetic);
            bloom.Alpha = light.Alpha * 0.8f;
            if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(2f, engine) && sprite.Visible)
            {
                flash.Play("flash", restart: true);
                flash.Visible = true;
            }
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                sprite.Visible = true;
                outline.Visible = false;
                Depth = -100;
                wiggler.Start();
                engine.SpatialAudio.Play("event:/new_content/game/10_farewell/pinkdiamond_return", Position);
                //level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
            }
        }

        private void UpdateY()
        {
            flash.Position.Y = sprite.Position.Y = bloom.Y = sine.Value * 2f;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (sprite.Visible)
            {
                sprite.DrawOutline(context);
            }

            base.Render(context);
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            engine.SpatialAudio.Play("event:/new_content/game/10_farewell/pinkdiamond_touch", Position);
            engine.RumbleSession.Rumble(1, 1);
            Collidable = false;
            timed = true;
            recallTimer += engine.DeltaTime;

            Add(new SimulationCoroutine(engine, RefillRoutine(player)));

            // COLOURSOFNOISE: Why not use this approach?
            //Add(new Coroutine(RecallRoutine(player)));
            respawnTimer = 2f + recallTime;
            if (refillDash)
            {
                player.RefillDash();
                player.RefillStamina();
            }
        }

        private IEnumerator RefillRoutine(RoomPlayerSession player)
        {
            engine.Freeze(0.05f);
            yield return null;
            level.Shake();
            sprite.Visible = (flash.Visible = false);
            if (!oneUse)
            {
                outline.Visible = true;
            }

            Depth = 8999;
            yield return 0.05f;
            float angle = Vector2Geometry.Angle(player.Speed);
            //level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle - (float)Math.PI / 2f);
            //level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle + (float)Math.PI / 2f);
            OriginalSlashFxState.Burst(engine, Position, angle);
            if (oneUse)
            {
                sprite.Visible = outline.Visible = false;
                Visible = false;
                Collidable = false;
                //RemoveSelf();
            }
        }

        private static void MoveFollowers(LeaderState leader, Vector2 offset)
        {
            for (int i = 0; i < leader.PastPoints.Count; i++)
            {
                leader.PastPoints[i] += offset;
            }

            foreach (FollowerState follower in leader.Followers)
            {
                ((ISourceMutableEntityPosition)follower.Entity).Position += offset;
            }
        }
    }
}
