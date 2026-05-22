namespace Wrecs.Core;

public static class ISystemExtensions
{
    extension(ISystem? system)
    {
        /// <summary>
        /// Performs a full "tick" for this individual system,
        /// invoking <see cref="IPrepareInternalUpdates.PrepareInternalUpdates"/>
        /// and <see cref="ISystem.ApplyInternalUpdates"/> in sequence.
        /// </summary>
        public void Tick()
        {
            ArgumentNullException.ThrowIfNull(system);

            if (system is IPrepareInternalUpdates preparer)
            {
                preparer.PrepareInternalUpdates();
            }
            system.ApplyInternalUpdates();
        }
    }
}
