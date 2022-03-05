using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class ValidateContosoCustomerBehavior : IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>
    {
        private readonly ILogger log;

        public ValidateContosoCustomerBehavior(ILogger log = null)
        {
            this.log = log ?? NullLogger.Instance;
        }

        public Task<ContosoCustomerResponse> Handle(ContosoCustomerRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<ContosoCustomerResponse> next)
        {
            if (string.IsNullOrEmpty(request.MessageId))
            {
                log.LogError("MessageId is required! :(");

                // short-circuit by throwing an exception
                throw new ArgumentException("MessageId is required! :(");

                // short-circuit by not calling the next behavior
                //return default;
            }

            log.LogInformation("Behavior {Behavior} completed", this.GetType().Name);

            return next();
        }
    }
}
