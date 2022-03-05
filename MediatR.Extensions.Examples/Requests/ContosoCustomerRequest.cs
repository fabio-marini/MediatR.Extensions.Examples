using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class ContosoCustomerResponse
    {
        public string MessageId { get; set; }
        public CanonicalCustomer CanonicalCustomer { get; set; }
    }

    public class ContosoCustomerRequest : IRequest<ContosoCustomerResponse>
    {
        public string MessageId { get; set; }
        public ContosoCustomer ContosoCustomer { get; set; }
    }

    public class ContosoCustomerHandler : IRequestHandler<ContosoCustomerRequest, ContosoCustomerResponse>
    {
        private readonly PipelineContext ctx;
        private readonly ILogger log;

        public ContosoCustomerHandler(PipelineContext ctx, ILogger log = null)
        {
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.log = log ?? NullLogger.Instance;
        }

        public Task<ContosoCustomerResponse> Handle(ContosoCustomerRequest request, CancellationToken cancellationToken)
        {
            if (ctx.ContainsKey(ContextKeys.CanonicalCustomer) == false)
            {
                throw new Exception("No canonical customer found in pipeline context");
            }

            var res = new ContosoCustomerResponse
            {
                MessageId = request.MessageId,
                CanonicalCustomer = (CanonicalCustomer)ctx[ContextKeys.CanonicalCustomer]
            };

            log.LogInformation("Handler {Handler} completed, returning", this.GetType().Name);

            return Task.FromResult(res);
        }
    }
}
