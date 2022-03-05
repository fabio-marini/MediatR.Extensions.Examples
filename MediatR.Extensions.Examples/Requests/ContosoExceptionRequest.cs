using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class ContosoExceptionRequest : IRequest<Unit>
    {
        public Exception Exception { get; set; }
        public ContosoCustomerRequest Request { get; set; }
    }

    public class ContosoExceptionHandler : IRequestHandler<ContosoExceptionRequest>
    {
        public Task<Unit> Handle(ContosoExceptionRequest request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
