using Celeste.Graphics.Animation;
using Celeste.Simulation.Gameplay.Collectibles.Refills;

namespace Celeste.Mods.JackalHelper.Entities.Refills
{
    public partial class ShroomRefill : IRefillTargetState
    {
        public Sprite RefillSprite => sprite;

        public float RefillRespawnTimer
        {
            get => respawnTimer;
            set => respawnTimer = value;
        }

        public bool RefillOneUse
        {
            get => oneUse;
            set => oneUse = value;
        }

        public void RespawnRefill() => Respawn();
    }
}
