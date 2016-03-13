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
        Pink,
        Grey,
        Maroon,
        Lime,
        Aqua,
        Navy,
        Fuchsia
    }

    public interface IPlayer
    {
        String UserId { get; }
        String Name { get; }
        PlayerColour Colour { get; }
    }
}
