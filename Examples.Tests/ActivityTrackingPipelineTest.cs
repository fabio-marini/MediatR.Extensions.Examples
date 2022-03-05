using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MediatR.Extensions.Examples
{
    [Trait("TestCategory", "Integration"), Collection("Examples")]
    [TestCaseOrderer("MediatR.Extensions.Tests.TestMethodNameOrderer", "Timeless.Testing.Xunit")]
    public class ActivityTrackingPipelineTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TableFixture tableFixture;
        private readonly string correlationId;

        public ActivityTrackingPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoActivityTrackingPipeline()
                .AddFabrikamActivityTrackingPipeline()

                .BuildServiceProvider();

            tableFixture = serviceProvider.GetRequiredService<TableFixture>();

            correlationId = "b4702445-613d-4787-b91d-4461c3bd4a4e";
        }

        [Fact(DisplayName = "01. Activities table is empty")]
        public void Step01() => tableFixture.GivenTableIsEmpty();

        [Fact(DisplayName = "02. Contoso pipeline is executed")]
        public async Task Step02()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new ContosoCustomerRequest
            {
                MessageId = correlationId,
                ContosoCustomer = new ContosoCustomer
                {
                    FirstName = "Fabio",
                    LastName = "Marini",
                    Email = "fm@example.com"
                }
            };

            var res = await med.Send(req);

            res.MessageId.Should().Be(req.MessageId);
        }

        [Fact(DisplayName = "03. Activities table has entities")]
        public void Step03() => tableFixture.ThenTableHasEntities(2);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = correlationId,
                CanonicalCustomer = new CanonicalCustomer
                {
                    FullName = "Fabio Marini",
                    Email = "fm@example.com"
                }
            };

            var res = await med.Send(req);

            res.MessageId.Should().Be(req.MessageId);
        }

        [Fact(DisplayName = "05. Activities table has entities")]
        public void Step05() => tableFixture.ThenTableHasEntities(4);

        [Fact(DisplayName = "06. Activity entities are merged")]
        public void Step06() => tableFixture.ThenEntitiesAreMerged(correlationId);
    }
}
