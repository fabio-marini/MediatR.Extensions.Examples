using FluentAssertions;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.IO;
using System.Linq;

namespace MediatR.Extensions.Examples
{
    public class FolderFixture
    {
        private readonly DirectoryInfo dir;
        private readonly ILogger log;

        public FolderFixture(DirectoryInfo dir, ILogger log = null)
        {
            this.dir = dir;
            this.log = log;
        }

        public void GivenFolderIsEmpty()
        {
            var allFiles = dir.GetFiles();

            if (allFiles.Any() == false)
            {
                log.LogInformation($"Fodler {dir.Name} has no files to delete");

                return;
            }

            foreach (var f in allFiles)
            {
                log.LogInformation($"Deleted file {f.Name} from folder {dir.Name}");

                File.Delete(f.FullName);
            }
        }

        public void ThenFolderHasFiles(int expectedCount)
        {
            var retryPolicy = Policy
                .HandleResult<int>(res => res != expectedCount)
                .WaitAndRetry(5, i => TimeSpan.FromSeconds(1));

            var actualCount = retryPolicy.Execute(() =>
            {
                var res = dir.GetFiles().Count();

                log.LogInformation($"Folder {dir.Name} has {res} files");

                return res;
            });

            actualCount.Should().Be(expectedCount);
        }

        public void ThenContainerIsEmpty() => ThenFolderHasFiles(0);

    }
}
