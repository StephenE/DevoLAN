using System;

namespace Peril.Core
{
    public enum PlayerColour
    {
        Black,
        Blue,
        Green,
        Red,
        Orange,
        Yellow,
        Purple,
        Pink
    }

    public interface IPlayer
    {
        String UserId { get; }
        String Name { get; }
        PlayerColour Colour { get; }
    }
}
