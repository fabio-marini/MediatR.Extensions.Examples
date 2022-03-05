using Azure.Messaging.ServiceBus;
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
    public class ServiceBusQueuePipelineTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ManagementFixture adminFixture;
        private const string MediatorQueue = "mediator-queue";

        public ServiceBusQueuePipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoSenderPipeline()
                .AddFabrikamReceiverPipeline()

                .AddTransient<ServiceBusSender>(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();

                    return client.CreateSender(MediatorQueue);
                })
                .AddTransient<ServiceBusReceiver>(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();

                    var options = new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };

                    return client.CreateReceiver(MediatorQueue, options);
                })

                .BuildServiceProvider();

            adminFixture = serviceProvider.GetRequiredService<ManagementFixture>();
        }

        [Fact(DisplayName = "01. Queue is recreated")]
        public async Task Step01() => await adminFixture.QueueIsRecreated(MediatorQueue);

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

            res.MessageId.Should().Be(req.MessageId);
        }

        [Fact(DisplayName = "03. Queue has messages")]
        public async Task Step03() => await adminFixture.QueueHasMessages(MediatorQueue, 1);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString()
            };

            var res = await med.Send(req);

            res.MessageId.Should().Be(req.MessageId);
        }

        [Fact(DisplayName = "05. Queue has messages")]
        public async Task Step05() => await adminFixture.QueueHasMessages(MediatorQueue, 0);
    }
}
