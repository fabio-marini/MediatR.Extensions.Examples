using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Linq;

namespace MediatR.Extensions.Examples
{
    public class BlobFixture
    {
        private readonly BlobContainerClient blb;
        private readonly ILogger log;

        public BlobFixture(BlobContainerClient blb, ILogger log = null)
        {
            this.blb = blb;
            this.log = log;
        }

        public void GivenContainerIsEmpty()
        {
            var allBlobs = blb.GetBlobs();

            if (allBlobs.Any() == false)
            {
                log.LogInformation($"Container {blb.Name} has no blobs to delete");

                return;
            }

            foreach (var b in allBlobs)
            {
                var res = blb.DeleteBlob(b.Name);

                res.Status.Should().Be(202);

                log.LogInformation($"Deleted blob {b.Name} from container {blb.Name}");
            }
        }

        public void ThenContainerHasBlobs(int expectedCount)
        {
            var retryPolicy = Policy
                .HandleResult<int>(res => res != expectedCount)
                .WaitAndRetry(5, i => TimeSpan.FromSeconds(1));

            var actualCount = retryPolicy.Execute(() =>
            {
                var res = blb.GetBlobs().Count();

                log.LogInformation($"Container {blb.Name} has {res} blobs");

                return res;
            });

            actualCount.Should().Be(expectedCount);
        }

        public void ThenContainerIsEmpty() => ThenContainerHasBlobs(0);

    }
}
