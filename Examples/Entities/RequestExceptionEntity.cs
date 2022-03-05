using Azure.Storage.Blobs;
using MediatR.Extensions.Abstractions;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace MediatR.Extensions.Examples
{
    public class RequestExceptionEntity : TableEntity
    {
        public static RequestExceptionEntity Create<TRequest>(TRequest req, PipelineContext ctx, Exception ex)
        {
            if (ctx.ContainsKey("BlobClient") == false)
            {
                throw new Exception("Context key 'BlobClient' not found! :(");
            }

            var blobClient = (BlobClient)ctx["BlobClient"];

            return new RequestExceptionEntity
            {
                PartitionKey = req.GetType().Name,
                RowKey = blobClient.Name.Replace("exceptions/", "").Replace(".json", ""),
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,
                AccountName = blobClient.AccountName,
                ContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                BlobUri = blobClient.Uri.ToString()
            };
        }

        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }

        public string AccountName { get; set; }
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
        public string BlobUri { get; set; }

    }
}
