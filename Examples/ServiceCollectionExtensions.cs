using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MediatR.Extensions.Azure.ServiceBus;
using MediatR.Extensions.Azure.Storage;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MediatR.Extensions.Examples
{
    public static class ServiceCollectionExtensions
    {
        // TODO: update all extensions test projects to use latest xunit logger (fix orderers namespaces)

        #region Examples TODOs and Ideas

        // TODO: extend admin fixture to check active (bug where active = 3 and scheduled = -3 happens when running all tests at once)

        // TODO: document pipelines in repo readme!

        // TODO: transaction/compensation scenario?
        // TODO: process expired message from DLQ + batching?
        // TODO: schedule/cancel: store sequence numbers in table + schedule multiple messages, only cancel some
        // TODO: repeat cancel tests with topics or use topics for "cancel some" tests?

        #endregion

        #region Other TODOs

        // TODO: SB docs + release

        // TODO: update storage extension commands and tests to use Invoke() on delegates that return a value?

        // TODO: simplify unit tests: call Verify() on all setup and remove manual verification +
        //       only verify that command method is called, e.g. SendAsync
        // TODO: use mock behavior strict (All invocations on the mock must have a corresponding setup)?
        // TODO: validate options in commands ctors using ?? (coalesce)

        // TODO: abstractions - should they rethrow the exception instead of adding it to ctx (make ctx not optional!)?

        // TODO: refactor all integration tests to use test fixtures and remove the need for orderers (create a snippet instead)

        #endregion

        #region Examples Overview and Other Extensions

        //  1. PipelineExecutionOnlyTests - models and pipelines
        //  2. MessageTrackingPipelineTest - blob message tracking pipeline (JSON and XML)
        //  3. ActivityTrackingPipelineTest - table activity tracking pipeline (JSON and XML)
        //  4. MessageClaimCheckPipelineTest - blob claim check pipeline
        //  5. ExceptionHandlingPipelineTest - error pipelines
        //  6. ServiceBusQueuePipelineTest - send/receive using a SB queue
        //  7. ServiceBusTopicPipelineTest - send/receive using a SB topic/subscription
        //  8. ScheduleAndProcessPipelineTest - schedule message and process
        //  9. ScheduleAndCancelPipelineTest - schedule message and cancel

        // TODO: HTTP request/response messages?
        // TODO: persistence points to enable edit and resubmit?
        // TODO: sign/verify and encrypt/decrypt using certs?

        #endregion

        public static IServiceCollection AddContosoRequestPipeline(this IServiceCollection services)
        {
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();

            return services;
        }

        public static IServiceCollection AddFabrikamRequestPipeline(this IServiceCollection services)
        {
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, TransformFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, EnrichFabrikamCustomerBehavior>();

            return services;
        }

        public static IServiceCollection AddContosoExceptionPipeline(this IServiceCollection services)
        {
            services.AddOptions<BlobOptions<ContosoExceptionRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) =>
                {
                    var blobClient = blb.GetBlobClient($"exceptions/{Guid.NewGuid().ToString()}.json");

                    // store blob client in context so it can be accessed by table options...
                    ctx.Add("BlobClient", blobClient);

                    return blobClient;
                };
                opt.BlobContent = (req, ctx) => BinaryData.FromString(JsonConvert.SerializeObject(req.Request));
            });
            services.AddOptions<TableOptions<ContosoExceptionRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (req, ctx) => RequestExceptionEntity.Create(req, ctx, req.Exception);
            });

            services.AddTransient<UploadBlobCommand<ContosoExceptionRequest>>();
            services.AddTransient<InsertEntityCommand<ContosoExceptionRequest>>();

            services.AddTransient<IPipelineBehavior<ContosoExceptionRequest, Unit>, UploadBlobRequestBehavior<ContosoExceptionRequest>>();
            services.AddTransient<IPipelineBehavior<ContosoExceptionRequest, Unit>, InsertEntityRequestBehavior<ContosoExceptionRequest>>();

            return services;
        }

        public static IServiceCollection AddFabrikamExceptionPipeline(this IServiceCollection services)
        {
            services.AddOptions<BlobOptions<FabrikamExceptionRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) =>
                {
                    var blobClient = blb.GetBlobClient($"exceptions/{Guid.NewGuid().ToString()}.json");

                    // store blob client in context so it can be accessed by table options...
                    ctx.Add("BlobClient", blobClient);

                    return blobClient;
                };
                opt.BlobContent = (req, ctx) => BinaryData.FromString(JsonConvert.SerializeObject(req.Request));
            });
            services.AddOptions<TableOptions<FabrikamExceptionRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (req, ctx) => RequestExceptionEntity.Create(req, ctx, req.Exception);
            });

            services.AddTransient<UploadBlobCommand<FabrikamExceptionRequest>>();
            services.AddTransient<InsertEntityCommand<FabrikamExceptionRequest>>();

            services.AddTransient<IPipelineBehavior<FabrikamExceptionRequest, Unit>, UploadBlobRequestBehavior<FabrikamExceptionRequest>>();
            services.AddTransient<IPipelineBehavior<FabrikamExceptionRequest, Unit>, InsertEntityRequestBehavior<FabrikamExceptionRequest>>();

            return services;
        }

        public static IServiceCollection AddContosoMessageTrackingPipeline(this IServiceCollection services)
        {
            services.AddOptions<BlobOptions<ContosoCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) => blb.GetBlobClient($"contoso/{Guid.NewGuid().ToString()}.json");
            });

            services.AddTransient<UploadBlobCommand<ContosoCustomerRequest>>();

            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, UploadBlobRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, UploadBlobRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddFabrikamMessageTrackingPipeline(this IServiceCollection services)
        {
            services.AddOptions<BlobOptions<FabrikamCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

        // use custom BlobContent to serialize only the canonical customer (as XML)
        opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) => blb.GetBlobClient($"fabrikam/{Guid.NewGuid().ToString()}.xml");
                opt.BlobContent = (req, ctx) =>
                {
                    var xml = new XmlSerializer(req.CanonicalCustomer.GetType());

                    using var ms = new MemoryStream();

                    xml.Serialize(ms, req.CanonicalCustomer);

                    return BinaryData.FromBytes(ms.ToArray());
                };
                opt.BlobHeaders = (req, ctx) => new BlobHttpHeaders { ContentType = "application/xml" };
            });

            services.AddTransient<UploadBlobCommand<FabrikamCustomerRequest>>();

            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, UploadBlobRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, TransformFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, EnrichFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, UploadBlobRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddContosoActivityTrackingPipeline(this IServiceCollection services)
        {
            services.AddOptions<TableOptions<ContosoCustomerRequest>>("Started").Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (req, ctx) =>
                {
                    return new CustomerActivityEntity
                    {
                        PartitionKey = req.CorrelationId,
                        RowKey = Guid.NewGuid().ToString(),
                        ContosoStarted = DateTime.Now,
                        Email = req.ContosoCustomer.Email
                    };
                };
            });
            services.AddOptions<TableOptions<ContosoCustomerRequest>>("Finished").Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (req, ctx) =>
                {
                    return new CustomerActivityEntity
                    {
                        PartitionKey = req.CorrelationId,
                        RowKey = Guid.NewGuid().ToString(),
                        IsValid = true,
                        ContosoFinished = DateTime.Now,
                    };
                };
            });

            services.AddTransient<InsertEntityCommand<ContosoCustomerRequest>>();

            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, InsertEntityRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>(sp =>
            {
                var opt = sp.GetRequiredService<IOptionsSnapshot<TableOptions<ContosoCustomerRequest>>>().Get("Started");

                var cmd = ActivatorUtilities.CreateInstance<InsertEntityCommand<ContosoCustomerRequest>>(sp, Options.Create(opt));

                return ActivatorUtilities.CreateInstance<InsertEntityRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>(sp, cmd);
            });
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, InsertEntityRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>(sp =>
            {
                var opt = sp.GetRequiredService<IOptionsSnapshot<TableOptions<ContosoCustomerRequest>>>().Get("Finished");

                var cmd = ActivatorUtilities.CreateInstance<InsertEntityCommand<ContosoCustomerRequest>>(sp, Options.Create(opt));

                return ActivatorUtilities.CreateInstance<InsertEntityRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>(sp, cmd);
            });

            return services;
        }

        public static IServiceCollection AddFabrikamActivityTrackingPipeline(this IServiceCollection services)
        {
            services.AddOptions<TableOptions<FabrikamCustomerRequest>>("Started").Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (req, ctx) =>
                {
                    return new CustomerActivityEntity
                    {
                        PartitionKey = req.CorrelationId,
                        RowKey = Guid.NewGuid().ToString(),
                        FabrikamStarted = DateTime.Now
                    };
                };
            });
            services.AddOptions<TableOptions<FabrikamCustomerResponse>>("Finished").Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var tbl = svc.GetRequiredService<CloudTable>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.CloudTable = tbl;
                opt.TableEntity = (res, ctx) =>
                {
                    return new CustomerActivityEntity
                    {
                        PartitionKey = res.CorrelationId,
                        RowKey = Guid.NewGuid().ToString(),
                        DateOfBirth = res.FabrikamCustomer.DateOfBirth,
                        FabrikamFinished = DateTime.Now
                    };
                };
            });

            services.AddTransient<InsertEntityCommand<FabrikamCustomerRequest>>();

            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, InsertEntityRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>(sp =>
            {
                var opt = sp.GetRequiredService<IOptionsSnapshot<TableOptions<FabrikamCustomerRequest>>>().Get("Started");

                var cmd = ActivatorUtilities.CreateInstance<InsertEntityCommand<FabrikamCustomerRequest>>(sp, Options.Create(opt));

                return ActivatorUtilities.CreateInstance<InsertEntityRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>(sp, cmd);
            });
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, TransformFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, EnrichFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, InsertEntityResponseBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>(sp =>
            {
                var opt = sp.GetRequiredService<IOptionsSnapshot<TableOptions<FabrikamCustomerResponse>>>().Get("Finished");

                var cmd = ActivatorUtilities.CreateInstance<InsertEntityCommand<FabrikamCustomerResponse>>(sp, Options.Create(opt));

                return ActivatorUtilities.CreateInstance<InsertEntityResponseBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>(sp, cmd);
            });

            return services;
        }

        public static IServiceCollection AddContosoClaimCheckPipeline(this IServiceCollection services)
        {
            services.AddTransient<UploadBlobCommand<ContosoCustomerRequest>>();

            services.AddOptions<BlobOptions<ContosoCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) => blb.GetBlobClient($"canonical/{req.CorrelationId}.json");
                opt.BlobContent = (req, ctx) =>
                {
                    // leave only the messageId
                    req.ContosoCustomer = null;

                    return BinaryData.FromString(JsonConvert.SerializeObject(req));
                };
            });

            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, UploadBlobRequestBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddFabrikamClaimCheckPipeline(this IServiceCollection services)
        {
            services.AddTransient<DownloadBlobCommand<FabrikamCustomerRequest>>();
            services.AddTransient<DeleteBlobCommand<FabrikamCustomerRequest>>();

            services.AddOptions<BlobOptions<FabrikamCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();
                var blb = svc.GetRequiredService<BlobContainerClient>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.BlobClient = (req, ctx) => blb.GetBlobClient($"canonical/{req.CorrelationId}.json");
                opt.Downloaded = (res, ctx, req) =>
                {
                    var canonicalCustomer = res.Content.ToString();

                    req.CanonicalCustomer = JsonConvert.DeserializeObject<CanonicalCustomer>(canonicalCustomer);

                    return Task.CompletedTask;
                };
            });

            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, DownloadBlobRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, DeleteBlobRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, TransformFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, EnrichFabrikamCustomerBehavior>();

            return services;
        }

        public static IServiceCollection AddContosoSenderPipeline(this IServiceCollection services)
        {
            // contoso puts a canonical message on a queue/topic and fabrikam receives it
            services.AddTransient<Azure.ServiceBus.SendMessageCommand<ContosoCustomerResponse>>();

            services.AddOptions<MessageOptions<ContosoCustomerResponse>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.Sender = svc.GetRequiredService<ServiceBusSender>();
                opt.Message = (req, ctx) => new ServiceBusMessage(JsonConvert.SerializeObject(req.CanonicalCustomer));
            });

            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, Azure.ServiceBus.SendMessageResponseBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddFabrikamReceiverPipeline(this IServiceCollection services)
        {
            services.AddTransient<Azure.ServiceBus.ReceiveMessageCommand<FabrikamCustomerRequest>>();

            services.AddOptions<MessageOptions<FabrikamCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.Receiver = svc.GetRequiredService<ServiceBusReceiver>();
                opt.Received = (msg, ctx, req) =>
                {
                    if (msg == null)
                    {
                        throw new ArgumentNullException("No message was received");
                    }

                    req.CanonicalCustomer = JsonConvert.DeserializeObject<CanonicalCustomer>(msg.Body.ToString());

                    return Task.CompletedTask;
                };
            });

            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, Azure.ServiceBus.ReceiveMessageRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, TransformFabrikamCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, EnrichFabrikamCustomerBehavior>();

            return services;
        }

        public static IServiceCollection AddContosoSchedulePipeline(this IServiceCollection services, double enqueueOffsetInSeconds)
        {
            services.AddTransient<ScheduleMessageCommand<ContosoCustomerResponse>>();

            services.AddOptions<MessageOptions<ContosoCustomerResponse>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.Sender = svc.GetRequiredService<ServiceBusSender>();
                opt.EnqueueTime = (req, ctx) => DateTimeOffset.UtcNow.AddSeconds(enqueueOffsetInSeconds);
                opt.Scheduled = (seq, msg, ctx, res) =>
                {
                    ctx.Add(res.CanonicalCustomer.Email, seq);

                    return Task.CompletedTask;
                };
            });

            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ValidateContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, TransformContosoCustomerBehavior>();
            services.AddTransient<IPipelineBehavior<ContosoCustomerRequest, ContosoCustomerResponse>, ScheduleMessageResponseBehavior<ContosoCustomerRequest, ContosoCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddFabrikamCancelPipeline(this IServiceCollection services)
        {
            services.AddTransient<CancelMessageCommand<FabrikamCustomerRequest>>();

            services.AddOptions<MessageOptions<FabrikamCustomerRequest>>().Configure<IServiceProvider>((opt, svc) =>
            {
                var cfg = svc.GetRequiredService<IConfiguration>();

                opt.IsEnabled = cfg.GetValue<bool>("TrackingEnabled");
                opt.Sender = svc.GetRequiredService<ServiceBusSender>();
                opt.SequenceNumber = (ctx, res) => (long)ctx[res.CanonicalCustomer.Email];
            });

            services.AddTransient<IPipelineBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>, CancelMessageRequestBehavior<FabrikamCustomerRequest, FabrikamCustomerResponse>>();

            return services;
        }

        public static IServiceCollection AddFabrikamSchedulePipeline(this IServiceCollection services)
        {
            // just process the delivered messages normally (i.e. those that were not cancelled)
            return services.AddFabrikamReceiverPipeline();
        }
    }
}