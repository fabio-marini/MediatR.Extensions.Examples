using System;

namespace MediatR.Extensions.Examples
{
    public class ContosoCustomer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class CanonicalCustomer
    {
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class FabrikamCustomer
    {
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
    }
}
