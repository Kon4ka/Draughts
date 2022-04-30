using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Draughts.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace ClientApp.Grpc
{
    internal sealed class DraughtsCallbackServiceImpl : DraughtsCallbackService.DraughtsCallbackServiceBase
    {

        public event Action<string> Identified;

        public override Task<Empty> IdentificationCallback(IdentificationRequestCallback request, ServerCallContext context)
        {
            Identified?.Invoke(request.Message);

            return Task.FromResult(new Empty());
        }

    }
}
