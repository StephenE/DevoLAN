using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Peril.Api.Models
{
    public class Session : ISession
    {
        public Guid GameId { get; set; }
    }
}