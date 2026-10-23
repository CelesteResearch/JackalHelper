// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display;
using Celeste.Mods.JackalHelper.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.JackalHelper.Entities.Refills
{
    [SceneTracked]
    [CustomEntity("TwigHelper/LargeRefill")]
    [RuntimeTypeAlias("Celeste.Mod.TwigHelper.Entities.ShroomRefill")]
    public partial class ShroomRefill : SceneEntityState
    {
        public static ParticleTypePresentation P_Shatter;
        public static ParticleTypePresentation P_Regen;
        public static ParticleTypePresentation P_Glow;
        public static ParticleTypePresentation P_ShatterTwo;
        public static ParticleTypePresentation P_RegenTwo;
        public static ParticleTypePresentation P_GlowTwo;
        private readonly IOriginalRefillProcess engine;
        private readonly TwigHelperModule module;
        private readonly Sprite sprite;
        private readonly SpriteImage outline;
        private readonly WigglerComponent wiggler;
        private readonly BloomPointState bloom;
        private readonly VertexLightState light;
        private readonly SineWaveState sine;
        private bool oneUse;
        private IOriginalRefillScene level;
        private float respawnTimer;
        public Celeste.Simulation.Gameplay.Environment.Wind.RoomWindPattern Pattern;
        public bool check;
        public bool refillDash;
        public bool refillStamina;
        public bool last;
        public bool windNext;
        public Vector2 lastPos;
        public bool legacy;

        public ShroomRefill(Vector2 position, bool oneUse, bool refillDash, bool refillStamina, bool legacy,
            IOriginalRefillProcess engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            module = TwigHelperModule.FindOwner(engine);
            this.legacy = legacy;
            this.oneUse = oneUse;
            this.refillDash = refillDash;
            this.refillStamina = refillStamina;
            Collider = new HitboxColliderGeometry(16, 16, -8, -8);
            Add(new PlayerColliderComponentState(OnPlayer));
            Add(outline = new SpriteImage(engine.GameGraphics.GameAtlas["objects/mushroomThing/outline"]));
            outline.CenterOrigin();
            outline.Visible = false;
            Add(sprite = module.CreateShroomSprite("shroomy"));
            sprite.Play("idle");
            sprite.CenterOrigin();
            Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 4f,
                v => sprite.Scale = Vector2.One * (1f + v * .2f)));
            Add(new MirrorReflectionState());
            Add(bloom = new BloomPointState(.8f, 16));
            Add(light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, 1, 16, 48));
            Add(sine = new SineWaveState(engine, .6f).Randomize());
            UpdateY();
            Depth = -100;
            module.Session.HasShroomDash = false;
            module.Session.ShroomDashActive = false;
        }

        public ShroomRefill(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(data.Position + offset,
            data.Bool("oneUse"), data.Bool("refillDash", true), data.Bool("refillStamina", true), data.Bool("legacy"),
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
            if (respawnTimer > 0)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0) Respawn();
            }

            UpdateY();
            light.Alpha = ScalarMath.Approach(light.Alpha, sprite.Visible ? 1 : 0, 4 * engine.DeltaTime,
                engine.ExtremaArithmetic);
            bloom.Alpha = light.Alpha * .8f;
        }

        private void Respawn()
        {
            if (Collidable) return;
            Collidable = true;
            sprite.Visible = true;
            outline.Visible = false;
            Depth = -100;
            wiggler.Start();
            engine.SpatialAudio.Play("event:/TwigHelper/diamond_return", Position);
        }

        private void UpdateY()
        {
            float y = bloom.Y = sine.Value * 2;
            sprite.Position.Y = y;
            sprite.Position.Y = y;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (sprite.Visible) sprite.DrawOutline(context);
            base.Render(context);
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (module.Session.HasShroomDash && !legacy) return;
            engine.SpatialAudio.Play("event:/TwigHelper/diamond_touch", Position);
            engine.RumbleSession.Rumble(1, 1);
            Collidable = false;
            Add(new SimulationCoroutine(engine, RefillRoutine(player)));
            respawnTimer = 2.5f;
            if (refillDash) player.RefillDash();
            if (refillStamina) player.RefillStamina();
            module.Session.HasShroomDash = true;
        }

        private IEnumerator RefillRoutine(RoomPlayerSession player)
        {
            engine.Freeze(.05f);
            yield return null;
            level.Shake();
            sprite.Visible = false;
            if (!oneUse) outline.Visible = true;
            Depth = 8999;
            yield return .05f;
            float angle = Vector2Geometry.Angle(player.Speed);
            OriginalSlashFxState.Burst(engine, Position, angle);
            if (oneUse) RemoveSelf();
        }
    }
}
