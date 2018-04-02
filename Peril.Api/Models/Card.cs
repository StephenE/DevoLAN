using Peril.Api.Repository;
using Peril.Core;
using System;

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