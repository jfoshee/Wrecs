namespace Wrecs.Core;

public static class SystemExtensions
{
    extension(IInternalUpdateSystem? system)
    {
        /// <summary>
        /// Performs a full "tick" for this individual system,
        /// invoking <see cref="IPrepareInternalUpdates.PrepareInternalUpdates"/>
        /// and <see cref="IApplyInternalUpdates.ApplyInternalUpdates"/> in sequence.
        /// </summary>
        public void Tick()
        {
            ArgumentNullException.ThrowIfNull(system);

            system.PrepareInternalUpdates();
            system.ApplyInternalUpdates();
        }
    }
}
