using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Model.Exceptions;

internal class BlobTagCountException:Exception
{
    public string Operation { get; }
    public string Message { get; }

    public BlobTagCountException(string operation, string message, Exception innerException)
        : base($"{message}", innerException)
    {
        Operation = operation;
        Message = message;
    }
}
