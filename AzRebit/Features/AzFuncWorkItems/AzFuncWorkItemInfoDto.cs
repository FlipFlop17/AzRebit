using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzRebit.Features.AzFuncWorkItems;

internal record AzFuncWorkItemInfoDto(string azureFunction,string invocationId,string fileName,int resubmitCount);
