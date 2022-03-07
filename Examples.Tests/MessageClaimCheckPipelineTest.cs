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
    public class MessageClaimCheckPipelineTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly BlobFixture blobFixture;
        private readonly string correlationId;

        public MessageClaimCheckPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoClaimCheckPipeline()
                .AddFabrikamClaimCheckPipeline()

                .BuildServiceProvider();

            blobFixture = serviceProvider.GetRequiredService<BlobFixture>();

            correlationId = "5e6d7294-967e-4612-92e0-485aeecdde54";
        }

        [Fact(DisplayName = "01. Claim checks container is empty")]
        public void Step01() => blobFixture.GivenContainerIsEmpty();

        [Fact(DisplayName = "02. Contoso pipeline is executed")]
        public async Task Step02()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new ContosoCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
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

        [Fact(DisplayName = "03. Claim checks container has blobs")]
        public void Step03() => blobFixture.ThenContainerHasBlobs(1);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,
            };

            var res = await med.Send(req);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "05. Claim checks container is empty")]
        public void Step05() => blobFixture.ThenContainerHasBlobs(0);
    }
}
