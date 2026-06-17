using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public interface ICommercialAgent :
    ICommercialEntity,
    IAgent,
    IRequireSnapshot<MoneySnapshot>,
    IRequireSnapshot<InventorySnapshot>,
    IRequireSnapshot<OfferListSnapshot>;
