using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace AzRebit.Shared;

internal interface ITriggersServiceCollection
{
    public void RegisterServices(IServiceCollection services);
}
