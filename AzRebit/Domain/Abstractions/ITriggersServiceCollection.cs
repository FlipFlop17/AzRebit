using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Domain.Abstractions;

internal interface ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services);
}
