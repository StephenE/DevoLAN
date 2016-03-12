using Peril.Core;
using System;

namespace Peril.Api.Models
{
    public class Player : IPlayer
    {
        public String UserId { get; set; }

        public String Name { get; set; }

        public PlayerColour Colour { get; set; }
    }
}