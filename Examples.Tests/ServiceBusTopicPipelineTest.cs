using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MediatR.Extensions.Examples
{
    [Trait("TestCategory", "Integration"), Collection("Examples")]
    [TestCaseOrderer("MediatR.Extensions.Tests.TestMethodNameOrderer", "Timeless.Testing.Xunit")]
    public class ServiceBusTopicPipelineTest : IAsyncDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly AdminFixture adminFixture;
        private const string MediatorTopic = "mediator-topic";
        private const string MediatorSubscription = "mediator-subscription";

        public ServiceBusTopicPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoSenderPipeline()
                .AddFabrikamReceiverPipeline()

                .AddTransient<ServiceBusSender>(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();

                    return client.CreateSender(MediatorTopic);
                })
                .AddTransient<ServiceBusReceiver>(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();

                    var options = new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };

                    return client.CreateReceiver(MediatorTopic, MediatorSubscription, options);
                })

                .BuildServiceProvider();

            adminFixture = serviceProvider.GetRequiredService<AdminFixture>();
        }

        [Fact(DisplayName = "01. Topic is recreated")]
        public async Task Step01() => await adminFixture.TopicIsRecreated(MediatorTopic, MediatorSubscription);

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

        [Fact(DisplayName = "03. Subscription has messages")]
        public async Task Step03() => await adminFixture.SubscriptionHasMessages(MediatorTopic, MediatorSubscription, 1);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString()
            };

            // cancel operation if no message is received within the specified delay
            var src = new CancellationTokenSource(5000);

            var res = await med.Send(req, src.Token);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "05. Subscription has messages")]
        public async Task Step05() => await adminFixture.SubscriptionHasMessages(MediatorTopic, MediatorSubscription, 0);

        public async ValueTask DisposeAsync()
        {
            await serviceProvider.GetRequiredService<ServiceBusSender>().CloseAsync();
            await serviceProvider.GetRequiredService<ServiceBusReceiver>().CloseAsync();
        }
    }
}
