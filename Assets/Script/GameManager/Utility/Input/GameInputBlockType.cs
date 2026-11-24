using System;

[Flags]
public enum GameInputBlockType
{
    None = 0,
    Gameplay = 1 << 0,
    UI = 1 << 1,
    All = Gameplay | UI
}
