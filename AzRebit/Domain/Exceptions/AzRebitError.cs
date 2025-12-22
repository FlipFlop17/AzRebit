using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Domain.Exceptions;

public enum AzRebitErrorType
{
    None,
    BlobResubmitFileNotFound,
    UnexpectedError,
    NotFound
}
