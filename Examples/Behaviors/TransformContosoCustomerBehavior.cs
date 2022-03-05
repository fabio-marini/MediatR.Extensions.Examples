using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class TransformContosoCustomerBehavior : IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>
    {
        private readonly PipelineContext ctx;
        private readonly ILogger log;

        public TransformContosoCustomerBehavior(PipelineContext ctx, ILogger log = null)
        {
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.log = log ?? NullLogger.Instance;
        }

        public Task<ContosoCustomerResponse> Handle(ContosoCustomerRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<ContosoCustomerResponse> next)
        {
            var canonicalCustomer = new CanonicalCustomer
            {
                FullName = $"{request.ContosoCustomer.FirstName} {request.ContosoCustomer.LastName}",
                Email = request.ContosoCustomer.Email
            };

            ctx.Add(ContextKeys.CanonicalCustomer, canonicalCustomer);

            log.LogInformation("Behavior {Behavior} completed", this.GetType().Name);

            return next();
        }
    }
}
