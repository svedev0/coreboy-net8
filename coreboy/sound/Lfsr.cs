namespace coreboy.sound;

public class Lfsr
{
    public int Value { get; private set; }

    public Lfsr() => Reset();
    public void Start() => Reset();
    public void Reset() => Value = 0x7fff;

    public int NextBit(bool widthMode7)
    {
        bool x = ((Value & 1) ^ ((Value & 2) >> 1)) != 0;
        Value >>= 1;
        Value |= (x ? (1 << 14) : 0);
        
        if (widthMode7)
        {
            Value |= (x ? (1 << 6) : 0);
        }

        return 1 & ~Value;
    }
}
