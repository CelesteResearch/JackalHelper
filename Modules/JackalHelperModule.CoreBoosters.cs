// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System.Xml;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.JackalHelper.Modules
{
    public sealed partial class JackalHelperModule
    {
        private SpriteBank coreBoosterSpriteBank;

        public override void LoadContent(bool firstLoad)
        {
            var xml = new XmlDocument();
            using (var stream = ((IEverestContentRegistryEnvironment)SourceEngine).SourceContentRegistry
                   .Get("Graphics/JackalHelper/Sprites").Stream)
                xml.Load(stream);
            coreBoosterSpriteBank = new SpriteBank(SourceEngine,
                ((ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics.GameAtlas, xml);
        }

        internal static Sprite CreateSprite(ISourceEngineContext engine, string id)
        {
            foreach (var candidate in ((Celeste.Everest.Modules.Runtime.ISourceEverestModulesEnvironment)engine)
                     .Modules)
                if (candidate is JackalHelperModule module)
                    return module.coreBoosterSpriteBank.Create(id);
            throw new System.InvalidOperationException("The owning JackalHelper module is not loaded.");
        }

        internal Sprite CreateCoreBoosterSprite(bool red) =>
            coreBoosterSpriteBank.Create(red ? "coreBoosterHot" : "coreBoosterCold");

        internal static OriginalLevelSceneRoot GetCurrentLevel(ISourceEngineContext engine) =>
            engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;

        internal static RoomPlayerSession GetCurrentPlayer(ISourceEngineContext engine)
        {
            var level = GetCurrentLevel(engine);
            if (level == null) return null;
            var players =
                ((ISourceScene)level).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession).FullName);
            return players.Count == 0 ? null : (RoomPlayerSession)players[0];
        }
    }
}
