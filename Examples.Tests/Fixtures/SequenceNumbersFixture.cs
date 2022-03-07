using System.Collections.Generic;

namespace MediatR.Extensions.Examples
{
    public class SequenceNumbersFixture
    {
        public SequenceNumbersFixture()
        {
            SequenceNumbers = new Dictionary<string, long>();
        }

        public Dictionary<string, long> SequenceNumbers { get; }
    }
}
