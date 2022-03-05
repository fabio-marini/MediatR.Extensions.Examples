using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class TransformFabrikamCustomerBehavior : IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>
    {
        private readonly PipelineContext ctx;
        private readonly ILogger log;

        public TransformFabrikamCustomerBehavior(PipelineContext ctx, ILogger log = null)
        {
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.log = log ?? NullLogger.Instance;
        }

        public Task<FabrikamCustomerResponse> Handle(FabrikamCustomerRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<FabrikamCustomerResponse> next)
        {
            var fabrikamCustomer = new FabrikamCustomer
            {
                FullName = request.CanonicalCustomer.FullName,
                Email = request.CanonicalCustomer.Email
            };

            ctx.Add(ContextKeys.FabrikamCustomer, fabrikamCustomer);

            log.LogInformation("Behavior {Behavior} completed", this.GetType().Name);

            return next();
        }
    }
}
