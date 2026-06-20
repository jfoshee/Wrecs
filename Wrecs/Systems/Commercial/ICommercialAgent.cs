using Wrecs.Core;

namespace Wrecs.Systems.Commercial;

public interface ICommercialAgent :
    ICommercialEntity,
    IAgent,
    IAgentRequireSnapshot<MoneySnapshot>,
    IAgentRequireSnapshot<InventorySnapshot>,
    IAgentRequireSnapshot<OfferListSnapshot>;
