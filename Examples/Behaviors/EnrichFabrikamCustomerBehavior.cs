using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class EnrichFabrikamCustomerBehavior : IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>
    {
        private readonly PipelineContext ctx;
        private readonly ILogger log;

        public EnrichFabrikamCustomerBehavior(PipelineContext ctx, ILogger log = null)
        {
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.log = log ?? NullLogger.Instance;
        }

        public Task<FabrikamCustomerResponse> Handle(FabrikamCustomerRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<FabrikamCustomerResponse> next)
        {
            if (string.IsNullOrEmpty(request.MessageId))
            {
                log.LogError("MessageId is required! :(");

                throw new ArgumentException("MessageId is required! :(");
            }

            if (ctx.ContainsKey(request.MessageId) == false)
            {
                throw new Exception("No Fabrikam customer found in pipeline context");
            }

            var fabrikamCustomer = (FabrikamCustomer)ctx[request.MessageId];

            fabrikamCustomer.DateOfBirth = new DateTime(1970, 10, 26);

            log.LogInformation("Behavior {Behavior} completed", this.GetType().Name);

            return next();
        }
    }
}
