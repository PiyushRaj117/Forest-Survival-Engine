using UnityEngine;

namespace ForestSurvival.Combat
{
    /// <summary>
    /// Catch and forward animation events (PlayStep, MeleeAttackStart, MeleeAttackEnd, Hit)
    /// dispatched by the Chomper model's Animator to EnemyAI on the root GameObject.
    /// </summary>
    public class ChomperAnimationEvents : MonoBehaviour
    {
        private EnemyAI enemyAI;

        private void Awake()
        {
            enemyAI = GetComponentInParent<EnemyAI>();
        }

        public void PlayStep()
        {
            if (enemyAI != null) enemyAI.PlayStep();
        }

        public void MeleeAttackStart()
        {
            if (enemyAI != null) enemyAI.MeleeAttackStart();
        }

        public void MeleeAttackEnd()
        {
            if (enemyAI != null) enemyAI.MeleeAttackEnd();
        }

        public void Hit()
        {
            if (enemyAI != null) enemyAI.Hit();
        }
    }
}
