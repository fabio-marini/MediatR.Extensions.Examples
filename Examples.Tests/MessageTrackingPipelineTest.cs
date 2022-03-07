using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MediatR.Extensions.Examples
{
    [Trait("TestCategory", "Integration"), Collection("Examples")]
    [TestCaseOrderer("Timeless.Testing.Xunit.TestMethodNameOrderer", "Timeless.Testing.Xunit")]
    public class MessageTrackingPipelineTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly BlobFixture blobFixture;

        public MessageTrackingPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoMessageTrackingPipeline()
                .AddFabrikamMessageTrackingPipeline()

                .BuildServiceProvider();

            blobFixture = serviceProvider.GetRequiredService<BlobFixture>();
        }

        [Fact(DisplayName = "01. Messages container is empty")]
        public void Step01() => blobFixture.GivenContainerIsEmpty();

        [Fact(DisplayName = "02. Contoso pipeline is executed")]
        public async Task Step02()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new ContosoCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                ContosoCustomer = new ContosoCustomer
                {
                    FirstName = "Fabio",
                    LastName = "Marini",
                    Email = "fm@example.com"
                }
            };

            var res = await med.Send(req);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "03. Messages container has blobs")]
        public void Step03() => blobFixture.ThenContainerHasBlobs(2);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                CanonicalCustomer = new CanonicalCustomer
                {
                    FullName = "Fabio Marini",
                    Email = "fm@example.com"
                }
            };

            var res = await med.Send(req);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "05. Messages container has blobs")]
        public void Step05() => blobFixture.ThenContainerHasBlobs(4);
    }
}
