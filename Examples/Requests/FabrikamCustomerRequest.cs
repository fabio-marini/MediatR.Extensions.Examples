using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class FabrikamCustomerResponse
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }

        public FabrikamCustomer FabrikamCustomer { get; set; }
    }

    public class FabrikamCustomerRequest : IRequest<FabrikamCustomerResponse>
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }

        public CanonicalCustomer CanonicalCustomer { get; set; }
    }

    public class FabrikamCustomerHandler : IRequestHandler<FabrikamCustomerRequest, FabrikamCustomerResponse>
    {
        private readonly PipelineContext ctx;
        private readonly ILogger log;

        public FabrikamCustomerHandler(PipelineContext ctx, ILogger log = null)
        {
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.log = log ?? NullLogger.Instance;
        }

        public Task<FabrikamCustomerResponse> Handle(FabrikamCustomerRequest request, CancellationToken cancellationToken)
        {
            if (ctx.ContainsKey(request.MessageId) == false)
            {
                throw new Exception("No Fabrikam customer found in pipeline context");
            }

            var res = new FabrikamCustomerResponse
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = request.CorrelationId,
                FabrikamCustomer = (FabrikamCustomer)ctx[request.MessageId]
            };

            log.LogInformation("Handler {Handler} completed, returning", this.GetType().Name);

            return Task.FromResult(res);
        }
    }
}
