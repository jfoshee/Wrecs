namespace Wrecs.Core;

public static class ISystemExtensions
{
    extension(ISystem? system)
    {
        /// <summary>
        /// Performs a full "tick" for this individual system,
        /// invoking <see cref="IPrepareInternalUpdates.PrepareInternalUpdates"/>
        /// and <see cref="IApplyInternalUpdates.ApplyInternalUpdates"/> in sequence.
        /// </summary>
        public void Tick()
        {
            ArgumentNullException.ThrowIfNull(system);

            if (system is IPrepareInternalUpdates preparer)
            {
                preparer.PrepareInternalUpdates();
            }
            if (system is IApplyInternalUpdates applier)
            {
                applier.ApplyInternalUpdates();
            }
        }
    }
}
