// Copyright (c) 2022 Jackal-Celeste. Licensed under the MIT License.

using System;
using System.Numerics;
using Celeste.Mods.JackalHelper.Sessions;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.JackalHelper.Modules
{
    public sealed partial class JackalHelperModule
    {
        private IDisposable climbJumpHook;
        private IDisposable climbUpdateHook;

        public override Type SessionType => typeof(JackalHelperSession);
        public JackalHelperSession Session => (JackalHelperSession)_Session;

        private void LoadStaminaLocks()
        {
            climbJumpHook = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.ClimbJump)),
                (Action<Action<RoomPlayerSession>, RoomPlayerSession>)ClimbJump);
            climbUpdateHook = new Hook(typeof(ClimbSequence).GetMethod(nameof(ClimbSequence.Update)),
                (Func<Func<ClimbSequence, int>, ClimbSequence, int>)ClimbUpdate);
        }

        private void UnloadStaminaLocks()
        {
            climbJumpHook?.Dispose();
            climbJumpHook = null;
            climbUpdateHook?.Dispose();
            climbUpdateHook = null;
        }

        private void RefillLockedStamina(RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, SourceEngine) &&
                !ClimbBlockerState.Check(player.Scene, player, player.Position + Vector2.UnitX * player.Data.Facing) &&
                Session.inStaminaZone)
                player.RefillStamina();
        }

        private void ClimbJump(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            RefillLockedStamina(player);
            original(player);
        }

        private int ClimbUpdate(Func<ClimbSequence, int> original, ClimbSequence sequence)
        {
            if (ReferenceEquals(sequence.EngineContext, SourceEngine) && sequence.World is RoomPlayerWorld world)
                RefillLockedStamina(world.SourcePlayer);
            return original(sequence);
        }
    }
}
