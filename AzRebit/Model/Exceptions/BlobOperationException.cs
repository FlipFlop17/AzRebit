using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Model.Exceptions;

internal class BlobOperationException:Exception
{
    public string Operation { get; }
    public string Description { get; }

    public BlobOperationException(string operation, string message, Exception innerException)
        : base($"{message}", innerException)
    {
        Operation = operation;
        Description = message;
    }
}
