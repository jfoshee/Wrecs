using Wrecs.Systems;

namespace Wrecs.Tests;

static class SimExtensions
{
    extension(Sim sim)
    {
        public int GetPosition(IEntity entity) => sim.GetSystem<Spatial1DSystem>().GetTypedState(entity).Position;
        public int GetMoneyBalance(IEntity entity) => sim.GetSystem<MoneySystem>().GetTypedState(entity).MoneyBalance;
        public int GetResourceBalance(IEntity entity, string resource) =>
            sim.GetSystem<InventorySystem>().GetTypedState(entity).GetAmount(resource);
    }

    extension(CommercialSim sim)
    {
        public CommercialSnapshot GetCommercialState(IEntity entity)
        {
            var moneyState = sim.GetSystem<MoneySystem>().GetTypedState(entity);
            var inventoryState = sim.GetSystem<InventorySystem>().GetTypedState(entity);
            return new CommercialSnapshot(moneyState, inventoryState);
        }
    }
}
