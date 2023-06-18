namespace LYGJ.EntitySystem.EnemyManagement {
    public sealed class Bandit : EnemyBase {

        #region Overrides of EnemyBase

        /// <inheritdoc />
        public override EnemyType Type => EnemyType.Bandit;

        #endregion

    }
}
