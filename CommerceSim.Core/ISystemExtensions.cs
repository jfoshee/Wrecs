namespace CommerceSim.Core;

public static class ISystemExtensions
{
    extension(ISystem? system)
    {
        /// <summary>
        /// Performs a full "tick" for this individual system,
        /// invoking <see cref="ISystem.PrepareUpdates"/> and <see cref="ISystem.ApplyUpdates"/> in sequence.
        /// </summary>
        public void Tick()
        {
            ArgumentNullException.ThrowIfNull(system);

            system.PrepareUpdates();
            system.ApplyUpdates();
        }
    }
}
