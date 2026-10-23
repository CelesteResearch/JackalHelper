// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.JackalHelper.Sessions;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.JackalHelper.Modules
{
    [RuntimeTypeAlias("TwigModule")]
    public sealed class TwigHelperModule : SourceEverestModule
    {
        public TwigHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override Type SessionType => typeof(TwigHelperSession);
        public TwigHelperSession Session => (TwigHelperSession)_Session;
        private static readonly PackedColor ShroomHairColor = new PackedColor(242, 0, 36);
        private static readonly PackedColor ShroomDashTrailColor = new PackedColor(242, 39, 69);
        private static readonly Func<PlayerHair, ISourceEngineContext> HairEngine = CreateHairEngineGetter();
        private readonly List<IDisposable> shroomHooks = new List<IDisposable>();
        private SpriteBank shroomSpriteBank;
        private float shroomLowSpeedTimer = .2f;

        internal static TwigHelperModule FindOwner(ISourceEngineContext engine)
        {
            foreach (var candidate in ((ISourceEverestModulesEnvironment)engine).Modules)
                if (candidate is TwigHelperModule module)
                    return module;
            throw new InvalidOperationException("The owning Twig module is not loaded.");
        }

        internal Sprite CreateShroomSprite(string id) => shroomSpriteBank.Create(id);

        public override void LoadContent(bool firstLoad)
        {
            var xml = new XmlDocument();
            using (var stream = ((IEverestContentRegistryEnvironment)SourceEngine).SourceContentRegistry
                   .Get("Graphics/TwigHelper/Sprites").Stream)
                xml.Load(stream);
            shroomSpriteBank = new SpriteBank(SourceEngine,
                ((ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics.GameAtlas, xml);
        }

        public override void Load()
        {
            try
            {
                shroomHooks.Add(new Hook(typeof(DashSequence).GetMethod(nameof(DashSequence.Begin)),
                    (Action<Action<DashSequence>, DashSequence>)ShroomDashBegin));
                shroomHooks.Add(new Hook(typeof(DashSequence).GetMethod(nameof(DashSequence.End)),
                    (Action<Action<DashSequence>, DashSequence>)ShroomDashEnd));
                shroomHooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)ShroomPlayerUpdate));
                shroomHooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Render)),
                    (Action<Action<RoomPlayerSession, ISourceRenderContext>, RoomPlayerSession, ISourceRenderContext>)
                    ShroomPlayerRender));
                shroomHooks.Add(new Hook(typeof(PlayerHair).GetMethod("GetHairColor", new[] { typeof(int) }),
                    (Func<Func<PlayerHair, int, PackedColor>, PlayerHair, int, PackedColor>)ShroomHair));
            }
            catch
            {
                Unload();
                throw;
            }
        }

        public override void Unload()
        {
            for (int i = shroomHooks.Count - 1; i >= 0; i--) shroomHooks[i].Dispose();
            shroomHooks.Clear();
        }

        private void ShroomDashBegin(Action<DashSequence> original, DashSequence sequence)
        {
            if (ReferenceEquals(sequence.EngineContext, SourceEngine))
            {
                if (Session.HasShroomDash)
                {
                    Session.ShroomDashActive = true;
                    Session.ShroomDashTrailActive = true;
                    sequence.Data.Motion.Normal.Speed *= 1.25f;
                }
                else Session.ShroomDashTrailActive = false;
            }

            original(sequence);
        }

        private void ShroomDashEnd(Action<DashSequence> original, DashSequence sequence)
        {
            original(sequence);
            if (ReferenceEquals(sequence.EngineContext, SourceEngine) && sequence.World is RoomPlayerWorld world &&
                world.SourcePlayer.Machine.State != 2 && Session.ShroomDashActive)
            {
                Session.ShroomDashActive = false;
                Session.HasShroomDash = false;
                sequence.Data.Motion.Normal.Speed *= 1.25f;
            }
        }

        private void ShroomPlayerUpdate(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, SourceEngine))
            {
                if (player.Speed.X < 64)
                {
                    shroomLowSpeedTimer -= SourceEngine.DeltaTime;
                    if (shroomLowSpeedTimer < 0) Session.ShroomDashTrailActive = false;
                }
                else shroomLowSpeedTimer = .2f;
            }

            original(player);
        }

        private void ShroomPlayerRender(Action<RoomPlayerSession, ISourceRenderContext> original,
            RoomPlayerSession player, ISourceRenderContext context)
        {
            original(player, context);
            if (ReferenceEquals(player.EngineContext, SourceEngine) &&
                (Session.HasShroomDash || Session.ShroomDashTrailActive))
            {
                var scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * player.Data.Facing, player.Sprite.Scale.Y);
                TrailManagerState.Add(SourceEngine, (ISourceRenderEnvironment)SourceEngine, player, scale,
                    ShroomDashTrailColor);
            }
        }

        private PackedColor ShroomHair(Func<PlayerHair, int, PackedColor> original, PlayerHair hair, int index)
        {
            return ReferenceEquals(HairEngine(hair), SourceEngine) && Session.HasShroomDash
                ? ShroomHairColor
                : original(hair, index);
        }

        private static Func<PlayerHair, ISourceEngineContext> CreateHairEngineGetter()
        {
            var field = typeof(PlayerHair).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance);
            var method = new DynamicMethod("ReadShroomHairOwner", typeof(ISourceEngineContext),
                new[] { typeof(PlayerHair) },
                typeof(TwigHelperModule).Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<PlayerHair, ISourceEngineContext>)method.CreateDelegate(
                typeof(Func<PlayerHair, ISourceEngineContext>));
        }
    }
}
