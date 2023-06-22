using Sirenix.OdinInspector;
using UnityEngine;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerAttacker : MonoBehaviour {
        // Two moves: Attack, Block

        [Title("Attacking")]
        [SerializeField, Tooltip("The player's attack damage multiplier."), Min(0), LabelText("Damage Multiplier")]
        float _AttackDamageMultiplier = 1f;

        void Start() {
            PlayerInput.Fire.Released += OnFireReleased;
        }

        void OnFireReleased( float Duration ) {
            if (PlayerEquipper.TryGetMeleeItem(out RuntimePlayerEquipment? Melee)) {
                Melee.InterceptFire(Duration, _AttackDamageMultiplier);
            }
            // TODO: Ranged
        }
    }
}
