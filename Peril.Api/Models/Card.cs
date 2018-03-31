using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Peril.Api.Models
{
    public class Card : ICard
    {
        public Guid RegionId { get; set; }
        public uint Value { get; set; }

        public Card(ICardData card)
        {
            RegionId = card.RegionId;
            Value = card.Value;
        }
    }
}