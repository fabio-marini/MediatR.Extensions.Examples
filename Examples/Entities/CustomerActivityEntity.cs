using Microsoft.Azure.Cosmos.Table;
using System;

namespace MediatR.Extensions.Examples
{
    public class CustomerActivityEntity : TableEntity
    {
        public string Email { get; set; }
        public bool? IsValid { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? ContosoStarted { get; set; }
        public DateTime? ContosoFinished { get; set; }
        public DateTime? FabrikamStarted { get; set; }
        public DateTime? FabrikamFinished { get; set; }
    }
}
