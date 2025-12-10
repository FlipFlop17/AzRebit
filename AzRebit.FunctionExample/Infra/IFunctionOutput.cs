using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.FunctionExample.Infra;

public interface IFunctionOutput
{
    Task PostOutputAsync(string message);
}
