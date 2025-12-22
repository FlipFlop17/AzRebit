using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Domain.Abstractions;

internal interface ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services);
}
