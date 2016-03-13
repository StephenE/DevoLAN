using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException()
            : base()
        {
        }
        public ConcurrencyException(String message)
            : base(message)
        {
        }
        public ConcurrencyException(String message, Exception inner)
            : base(message, inner)
        {
        }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
