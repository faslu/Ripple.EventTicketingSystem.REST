using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Domain.Exceptions
{
    public class ValidationException : Exception
    { 
        public ValidationException(string message) 
            :base(message) 
        { 
        } 
    }
}
