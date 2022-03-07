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
    public class ScheduleAndProcessPipelineTest : IClassFixture<SequenceNumbersFixture>, IAsyncDisposable
    {
        // TODO: document pipelines in repo readme!

        // TODO: extend admin fixture to check active (bug where active = 3 and scheduled = -3
        //       happens when running all tests at once)

        // FIXME: xunit logger should print exception details; omit enqueue time to reproduce 
        //        (exception is thrown before any debug statements are printed!)
        // TODO: repeat cancel tests with topics or use topics for "cancel some" tests?

        // TODO: could this pipeline be redesigned as a transaction/compensation scenario?
        // FIXME: what happens when a receive command is executed against a queue with no messages?
        private readonly IServiceProvider serviceProvider;
        private readonly AdminFixture adminFixture;
        private const string MediatorQueue = "mediator-queue";
        private const double EnqueueOffset = 3;

        public ScheduleAndProcessPipelineTest(ITestOutputHelper log)
        {
            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoSchedulePipeline(EnqueueOffset)
                .AddFabrikamSchedulePipeline()

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

            adminFixture = serviceProvider.GetRequiredService<AdminFixture>();
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

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "03. Queue has scheduled messages")]
        public async Task Step03() => await adminFixture.QueueHasScheduledMessages(MediatorQueue, 1);

        [Fact(DisplayName = "04. Messages are delivered")]
        public async Task Step04()
        {
            await Task.Delay(((int)EnqueueOffset) * 1000);

            await adminFixture.QueueHasMessages(MediatorQueue, 1);
        }

        [Fact(DisplayName = "05. Fabrikam pipeline is executed")]
        public async Task Step05()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString()
            };

            var res = await med.Send(req);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "06. Queue has messages")]
        public async Task Step06() => await adminFixture.QueueHasMessages(MediatorQueue, 0);

        public async ValueTask DisposeAsync()
        {
            await serviceProvider.GetRequiredService<ServiceBusSender>().CloseAsync();
            await serviceProvider.GetRequiredService<ServiceBusReceiver>().CloseAsync();
        }
    }
}
