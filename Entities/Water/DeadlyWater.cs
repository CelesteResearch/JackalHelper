// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Environment.Water;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Water;
using Celeste.Mods.JackalHelper.Content.Colors;
using Celeste.Mods.JackalHelper.Triggers.Water;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.JackalHelper.Entities.Water
{
    [CustomEntity("JackalHelper/DeadlyWater")]
    [EverestTrackedAs(typeof(EverestWaterState))]
    public class DeadlyWater : EverestWaterState
    {
        private readonly DeadlyWaterAppearance appearance;
        private readonly bool hasRays;
        private bool fixedSurfaces;
        private bool visibleOnCamera;
        private List<Surface> emptySurfaces;
        private List<Surface> actualSurfaces;
        private Surface actualTopSurface;
        private Surface dummyTopSurface;
        private Surface actualBottomSurface;
        private Surface dummyBottomSurface;
        protected PlayerColliderComponentState playerCollider;
        protected HitboxColliderGeometry hitbox;
        protected float currentHeight;
        public KillBoxTrigger killbox;
        public float height;
        public float width;
        public IntRect rect;
        protected override PackedColor FillColor => appearance.FillColor;

        public static DeadlyWater Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public DeadlyWater(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            appearance = new DeadlyWaterAppearance(engine, JackalColorReader.GetColor(data.Attr("color", "#87CEFA")));
            hasRays = data.Bool("hasRays", false);
            currentHeight = data.Height;
            data.Height = (int)currentHeight;
            killbox = new KillBoxTrigger(engine, data, offset);
            Collidable = false;
            height = data.Height;
            width = data.Width;
            hitbox = new HitboxColliderGeometry(data.Width, currentHeight);
            rect = new IntRect((int)Position.X, (int)Position.Y, data.Width, data.Height);
            Add(playerCollider = new PlayerColliderComponentState(OnPlayer, hitbox));
        }

        private void FixSurfaces()
        {
            if (fixedSurfaces) return;
            bool hasTop = Surfaces.Contains(TopSurface);
            bool hasBottom = Surfaces.Contains(BottomSurface);
            Surfaces.Clear();
            if (hasTop)
            {
                TopSurface = new DeadlyWaterSurface(engine, Position + new Vector2(Width / 2f, 8f), new Vector2(0, -1),
                    Width, Height, appearance);
                Surfaces.Add(TopSurface);
                actualTopSurface = TopSurface;
                dummyTopSurface = new DeadlyWaterSurface(engine, Position + new Vector2(Width / 2f, 8f),
                    new Vector2(0, -1), Width, Height, appearance);
                if (!hasRays)
                {
                    actualTopSurface.Rays.Clear();
                    dummyTopSurface.Rays.Clear();
                }
            }

            if (hasBottom)
            {
                BottomSurface = new DeadlyWaterSurface(engine, Position + new Vector2(Width / 2f, Height - 8f),
                    new Vector2(0, 1), Width, Height, appearance);
                Surfaces.Add(BottomSurface);
                actualBottomSurface = BottomSurface;
                dummyBottomSurface = new DeadlyWaterSurface(engine, Position + new Vector2(Width / 2f, Height - 8f),
                    new Vector2(0, 1), Width, Height, appearance);
                if (!hasRays)
                {
                    actualBottomSurface.Rays.Clear();
                    dummyBottomSurface.Rays.Clear();
                }
            }

            fixedSurfaces = true;
            actualSurfaces = Surfaces;
            emptySurfaces = [];
        }

        private void UpdateSurfaces()
        {
            Surfaces = visibleOnCamera ? actualSurfaces : emptySurfaces;
            TopSurface = visibleOnCamera ? actualTopSurface : dummyTopSurface;
            BottomSurface = visibleOnCamera ? actualBottomSurface : dummyBottomSurface;
            if (!visibleOnCamera)
            {
                dummyTopSurface?.Ripples?.Clear();
                dummyBottomSurface?.Ripples?.Clear();
            }
        }

        private void UpdateVisibility(OriginalLevelSceneRoot level)
        {
            var camera = level.Camera;
            bool horizontalCheck = X < camera.Right + 48 && X + Width > camera.Left - 48;
            bool verticalCheck = Y < camera.Bottom + 48 && Y + Height > camera.Top - 48;
            visibleOnCamera = horizontalCheck && verticalCheck;
        }

        public bool MoistCheck(RoomPlayerSession player)
        {
            if (engine.Frame.Scenes.CurrentScene is not OriginalLevelSceneRoot) return false;
            if (player.BottomCenter.X > rect.X && player.BottomCenter.X < rect.X + Width)
            {
                if (player.TopCenter.Y < rect.Y + rect.Height && player.TopCenter.Y > rect.Y) return true;
                if (player.BottomCenter.Y < rect.Y + rect.Height && player.BottomCenter.Y > rect.Y) return true;
            }

            return false;
        }

        private void OnPlayer(RoomPlayerSession player) => player.Die(Vector2.Zero);

        public override void Update()
        {
            UpdateVisibility((OriginalLevelSceneRoot)Scene);
            UpdateSurfaces();
            base.Update();
            if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot current)
            {
                var players =
                    ((ISourceScene)current).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession).FullName);
                var player = players.Count > 0 ? (RoomPlayerSession)players[0] : null;
                if (player != null && MoistCheck(player)) player.Die(Vector2.Zero);
            }
        }

        public override void Added(object scene)
        {
            FixSurfaces();
            base.Added(scene);
        }

        public override void Render(Celeste.Core.Rendering.ISourceRenderContext context)
        {
            if (visibleOnCamera) base.Render(context);
        }
    }
}
