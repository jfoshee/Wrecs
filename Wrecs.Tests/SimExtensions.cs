using Wrecs.Systems;

namespace Wrecs.Tests;

static class SimExtensions
{
    extension(Sim sim)
    {
        public int GetPosition(IEntity entity) => sim.GetSystem<Spatial1DSystem>().GetState(entity).Position;
        public int GetMoneyBalance(IEntity entity) => sim.GetSystem<MoneySystem>().GetState(entity).MoneyBalance;
        public int GetResourceBalance(IEntity entity, string resource) =>
            sim.GetSystem<InventorySystem>().GetState(entity).GetAmount(resource);
    }

    extension(CommercialSim sim)
    {
        public CommercialSnapshot GetCommercialState(IEntity entity)
        {
            var moneyState = sim.GetSystem<MoneySystem>().GetState(entity);
            var inventoryState = sim.GetSystem<InventorySystem>().GetState(entity);
            return new CommercialSnapshot(moneyState, inventoryState);
        }
    }
}
