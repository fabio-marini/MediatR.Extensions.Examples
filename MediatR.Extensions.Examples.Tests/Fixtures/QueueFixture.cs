using Azure.Storage.Queues;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace MediatR.Extensions.Examples
{
    public class QueueFixture
    {
        private readonly QueueClient que;
        private readonly ILogger log;

        public QueueFixture(QueueClient que, ILogger log = null)
        {
            this.que = que;
            this.log = log;
        }

        public void GivenQueueIsEmpty()
        {
            que.ClearMessages();

            log.LogInformation($"Deleted all messages from queue {que.Name}");
        }

        public void ThenQueueHasMessages(int expectedCount)
        {
            var retryPolicy = Policy
                .HandleResult<int>(res => res != expectedCount)
                .WaitAndRetry(5, i => TimeSpan.FromSeconds(1));

            var actualCount = retryPolicy.Execute(() =>
            {
                var res = que.PeekMessages(32).Value.Length;

                log.LogInformation($"Queue {que.Name} has {res} messages");

                return res;
            });

            actualCount.Should().Be(expectedCount);
        }

        public void ThenQueueIsEmpty() => ThenQueueHasMessages(0);
    }
}
