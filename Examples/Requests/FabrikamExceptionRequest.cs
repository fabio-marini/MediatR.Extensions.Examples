using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class FabrikamExceptionRequest : IRequest<Unit>
    {
        public Exception Exception { get; set; }
        public FabrikamCustomerRequest Request { get; set; }
    }

    public class FabrikamExceptionHandler : IRequestHandler<FabrikamExceptionRequest>
    {
        public Task<Unit> Handle(FabrikamExceptionRequest request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
