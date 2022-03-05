using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Linq;

namespace MediatR.Extensions.Examples
{
    public class TableFixture
    {
        private readonly CloudTable tbl;
        private readonly ILogger log;

        public TableFixture(CloudTable tbl, ILogger log = null)
        {
            this.tbl = tbl;
            this.log = log;
        }

        public void GivenTableIsEmpty()
        {
            var allEntities = tbl.ExecuteQuery(new TableQuery());

            if (allEntities.Any() == false)
            {
                log.LogInformation($"Table {tbl.Name} has no entities to delete");

                return;
            }

            foreach (var e in allEntities)
            {
                var res = tbl.Execute(TableOperation.Delete(e));

                res.HttpStatusCode.Should().Be(204);

                log.LogInformation($"Deleted entity {e.RowKey} from table {tbl.Name}");
            }
        }

        public void ThenTableHasEntities(int expectedCount)
        {
            var retryPolicy = Policy
                .HandleResult<int>(res => res != expectedCount)
                .WaitAndRetry(5, i => TimeSpan.FromSeconds(1));

            var actualCount = retryPolicy.Execute(() =>
            {
                var res = tbl.ExecuteQuery(new TableQuery()).Count();

                log.LogInformation($"Table {tbl.Name} has {res} entities");

                return res;
            });

            actualCount.Should().Be(expectedCount);
        }

        public void ThenTableIsEmpty() => ThenTableHasEntities(0);

        public void ThenEntitiesAreMerged(string partitionKey)
        {
            var qry = new TableQuery<CustomerActivityEntity>().Where($"PartitionKey eq '{partitionKey}'");

            var entities = tbl.ExecuteQuery(qry);

            var merged = entities.OrderBy(e => e.Timestamp).Aggregate((e1, e2) =>
            {
                return new CustomerActivityEntity
                {
                    PartitionKey = partitionKey,
                    ContosoStarted = e1.ContosoStarted ?? e2.ContosoStarted,
                    ContosoFinished = e1.ContosoFinished ?? e2.ContosoFinished,
                    FabrikamStarted = e1.FabrikamStarted ?? e2.FabrikamStarted,
                    FabrikamFinished = e1.FabrikamFinished ?? e2.FabrikamFinished,
                    Email = e1.Email ?? e2.Email,
                    IsValid = e1.IsValid ?? e2.IsValid,
                    DateOfBirth = e1.DateOfBirth ?? e2.DateOfBirth,
                };
            });

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var obj = JObject.FromObject(merged, JsonSerializer.Create(settings));

            // this is not nullable and merging sets it to DateTime.Minvalue
            _ = obj.Remove("Timestamp");

            log.LogInformation(obj.ToString());
        }
    }
}
