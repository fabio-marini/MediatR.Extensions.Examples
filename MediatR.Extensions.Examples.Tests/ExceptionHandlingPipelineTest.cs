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
    public class ExceptionHandlingPipelineTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TableFixture tableFixture;
        private readonly BlobFixture blobFixture;

        public ExceptionHandlingPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)

                .AddContosoRequestPipeline()
                .AddContosoExceptionPipeline()

                .AddFabrikamRequestPipeline()
                .AddFabrikamExceptionPipeline()

                .BuildServiceProvider();

            tableFixture = serviceProvider.GetRequiredService<TableFixture>();
            blobFixture = serviceProvider.GetRequiredService<BlobFixture>();
        }

        [Fact(DisplayName = "01. Exceptions table is empty")]
        public void Step01() => tableFixture.GivenTableIsEmpty();

        [Fact(DisplayName = "02. Messages container is empty")]
        public void Step02() => blobFixture.GivenContainerIsEmpty();

        [Fact(DisplayName = "03. Contoso pipeline is executed")]
        public async Task Step03()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new ContosoCustomerRequest
            {
                ContosoCustomer = new ContosoCustomer
                {
                    FirstName = "Fabio",
                    LastName = "Marini",
                    Email = "fm@example.com"
                }
            };

            try
            {
                var res = await med.Send(req);
            }
            catch (Exception ex)
            {
                var err = new ContosoExceptionRequest
                {
                    Exception = ex,
                    Request = req
                };

                _ = await med.Send(err);
            }
        }

        [Fact(DisplayName = "04. Exceptions table has entities")]
        public void Step04() => tableFixture.ThenTableHasEntities(1);

        [Fact(DisplayName = "05. Messages container has blobs")]
        public void Step05() => blobFixture.ThenContainerHasBlobs(1);

        [Fact(DisplayName = "06. Fabrikam pipeline is executed")]
        public async Task Step06()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                CanonicalCustomer = new CanonicalCustomer
                {
                    FullName = "Fabio Marini",
                    Email = "fm@example.com"
                }
            };

            try
            {
                var res = await med.Send(req);
            }
            catch (Exception ex)
            {
                var err = new FabrikamExceptionRequest
                {
                    Exception = ex,
                    Request = req
                };

                _ = await med.Send(err);
            }
        }

        [Fact(DisplayName = "07. Exceptions table has entities")]
        public void Step07() => tableFixture.ThenTableHasEntities(2);

        [Fact(DisplayName = "08. Messages container has blobs")]
        public void Step08() => blobFixture.ThenContainerHasBlobs(2);
    }
}
