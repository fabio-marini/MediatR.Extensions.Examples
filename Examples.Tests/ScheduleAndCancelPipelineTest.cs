using Azure.Messaging.ServiceBus;
using FluentAssertions;
using MediatR.Extensions.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MediatR.Extensions.Examples
{
    [Trait("TestCategory", "Integration"), Collection("Examples")]
    [TestCaseOrderer("MediatR.Extensions.Tests.TestMethodNameOrderer", "Timeless.Testing.Xunit")]
    public class ScheduleAndCancelPipelineTest : IClassFixture<SequenceNumbersFixture>, IAsyncDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceNumbersFixture sequenceNumbers;
        private readonly AdminFixture adminFixture;
        private const string MediatorQueue = "mediator-queue";
        private const double EnqueueOffset = 5;

        public ScheduleAndCancelPipelineTest(ITestOutputHelper log, SequenceNumbersFixture sequenceNumbers)
        {
            this.sequenceNumbers = sequenceNumbers;

            serviceProvider = new ServiceCollection()

                .AddCoreDependencies(log)
                .AddContosoSchedulePipeline(EnqueueOffset)
                .AddFabrikamCancelPipeline()

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

            var ctx = serviceProvider.GetRequiredService<PipelineContext>();

            sequenceNumbers.SequenceNumbers.Add(req.ContosoCustomer.Email, (long)ctx[req.ContosoCustomer.Email]);
        }

        [Fact(DisplayName = "03. Queue has scheduled messages")]
        public async Task Step03() => await adminFixture.QueueHasScheduledMessages(MediatorQueue, 1);

        [Fact(DisplayName = "04. Fabrikam pipeline is executed")]
        public async Task Step04()
        {
            var med = serviceProvider.GetRequiredService<IMediator>();

            var req = new FabrikamCustomerRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                CanonicalCustomer = new CanonicalCustomer
                {
                    Email = "fm@example.com"
                }
            };

            var ctx = serviceProvider.GetRequiredService<PipelineContext>();

            ctx[req.CanonicalCustomer.Email] = sequenceNumbers.SequenceNumbers[req.CanonicalCustomer.Email];

            // hacking the handler which expects one of these...
            ctx[req.MessageId] = default(FabrikamCustomer);

            var res = await med.Send(req);

            res.CorrelationId.Should().Be(req.CorrelationId);
        }

        [Fact(DisplayName = "05. Queue has scheduled messages")]
        public async Task Step05() => await adminFixture.QueueHasScheduledMessages(MediatorQueue, 0);

        [Fact(DisplayName = "06. Queue has messages")]
        public async Task Step06() => await adminFixture.QueueHasMessages(MediatorQueue, 0);

        public async ValueTask DisposeAsync()
        {
            await serviceProvider.GetRequiredService<ServiceBusSender>().CloseAsync();
            await serviceProvider.GetRequiredService<ServiceBusReceiver>().CloseAsync();
        }
    }
}
