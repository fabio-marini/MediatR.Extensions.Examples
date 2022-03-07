using Azure.Messaging.ServiceBus.Administration;
using FluentAssertions;
using Polly;
using System;
using System.Threading.Tasks;

namespace MediatR.Extensions.Examples
{
    public class AdminFixture
    {
        private readonly ServiceBusAdministrationClient adminClient;

        public AdminFixture(ServiceBusAdministrationClient adminClient)
        {
            this.adminClient = adminClient;
        }

        public async Task QueueIsRecreated(string queuePath)
        {
            if (await adminClient.QueueExistsAsync(queuePath))
            {
                var runtimeInfo = await adminClient.GetQueueRuntimePropertiesAsync(queuePath);

                if (runtimeInfo.Value.TotalMessageCount > 0)
                {
                    // only recreate queue if it has any messages...
                    await adminClient.DeleteQueueAsync(queuePath);

                    await adminClient.CreateQueueAsync(queuePath);
                }
            }
            else
            {
                await adminClient.CreateQueueAsync(queuePath);
            }
        }

        public async Task QueueHasMessages(string queuePath, int expectedCount)
        {
            var retryPolicy = Policy.HandleResult<long>(res => res != expectedCount)
                .WaitAndRetryAsync(5, x => TimeSpan.FromMilliseconds(500));

            var messageCount = await retryPolicy.ExecuteAsync(async () =>
            {
                var runtimeInfo = await adminClient.GetQueueRuntimePropertiesAsync(queuePath);

                return runtimeInfo.Value.TotalMessageCount;
            });

            messageCount.Should().Be(expectedCount);
        }

        public async Task QueueHasScheduledMessages(string queuePath, int expectedCount)
        {
            var retryPolicy = Policy.HandleResult<long>(res => res != expectedCount)
                .WaitAndRetryAsync(5, x => TimeSpan.FromMilliseconds(500));

            var messageCount = await retryPolicy.ExecuteAsync(async () =>
            {
                var runtimeInfo = await adminClient.GetQueueRuntimePropertiesAsync(queuePath);

                return runtimeInfo.Value.ScheduledMessageCount;
            });

            messageCount.Should().Be(expectedCount);
        }

        public async Task TopicHasScheduledMessages(string topicPath, int expectedCount)
        {
            var retryPolicy = Policy.HandleResult<long>(res => res != expectedCount)
                .WaitAndRetryAsync(5, x => TimeSpan.FromMilliseconds(500));

            var messageCount = await retryPolicy.ExecuteAsync(async () =>
            {
                var runtimeInfo = await adminClient.GetTopicRuntimePropertiesAsync(topicPath);

                return runtimeInfo.Value.ScheduledMessageCount;
            });

            messageCount.Should().Be(expectedCount);
        }

        public async Task TopicIsRecreated(string topicPath, string subscriptionName, CreateRuleOptions defaultRule = default)
        {
            if (await adminClient.TopicExistsAsync(topicPath) == false)
            {
                await adminClient.CreateTopicAsync(topicPath);
            }

            if (await adminClient.SubscriptionExistsAsync(topicPath, subscriptionName) == true)
            {
                await adminClient.DeleteSubscriptionAsync(topicPath, subscriptionName);
            }

            var subscriptionDescription = new CreateSubscriptionOptions(topicPath, subscriptionName);

            await adminClient.CreateSubscriptionAsync(subscriptionDescription, defaultRule ?? new CreateRuleOptions());
        }

        public async Task SubscriptionHasMessages(string topicPath, string subscriptionName, int expectedCount)
        {
            var retryPolicy = Policy.HandleResult<long>(res => res != expectedCount)
                .WaitAndRetryAsync(5, x => TimeSpan.FromMilliseconds(500));

            var messageCount = await retryPolicy.ExecuteAsync(async () =>
            {
                var runtimeInfo = await adminClient.GetSubscriptionRuntimePropertiesAsync(topicPath, subscriptionName);

                return runtimeInfo.Value.TotalMessageCount;
            });

            messageCount.Should().Be(expectedCount);
        }
    }
}
