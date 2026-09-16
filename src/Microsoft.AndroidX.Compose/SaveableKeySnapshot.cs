namespace AndroidX.Compose;

internal sealed class SaveableKeySnapshot
{
    const byte JavaPeer = 0;
    const byte String = 1;
    const byte Boolean = 2;
    const byte Character = 3;
    const byte Byte = 4;
    const byte Short = 5;
    const byte Integer = 6;
    const byte Long = 7;
    const byte Float = 8;
    const byte Double = 9;

    readonly byte kind;
    readonly object value;

    SaveableKeySnapshot(byte kind, object value)
    {
        this.kind = kind;
        this.value = value;
    }

    internal static SaveableKeySnapshot Create(object key) => key switch
    {
        Java.Lang.Object peer => new(JavaPeer, peer),
        string text => new(String, text),
        bool boolean => new(Boolean, boolean),
        char character => new(Character, character),
        sbyte number => new(Byte, number),
        byte number => new(Short, (short)number),
        short number => new(Short, number),
        ushort number => new(Integer, (int)number),
        int number => new(Integer, number),
        uint number => new(Long, (long)number),
        long number => new(Long, number),
        ulong number => new(Long, unchecked((long)number)),
        float number => new(Float, number),
        double number => new(Double, number),
        _ => new(String, key.ToString() ?? string.Empty),
    };

    internal Java.Lang.Object Box(out bool owns)
    {
        if (kind == JavaPeer)
        {
            owns = false;
            return (Java.Lang.Object)value;
        }

        owns = true;
        Java.Lang.Object? boxed = kind switch
        {
            String => new Java.Lang.String((string)value),
            Boolean => Java.Lang.Boolean.ValueOf((bool)value),
            Character => Java.Lang.Character.ValueOf((char)value),
            Byte => Java.Lang.Byte.ValueOf((sbyte)value),
            Short => Java.Lang.Short.ValueOf((short)value),
            Integer => Java.Lang.Integer.ValueOf((int)value),
            Long => Java.Lang.Long.ValueOf((long)value),
            Float => Java.Lang.Float.ValueOf((float)value),
            Double => Java.Lang.Double.ValueOf((double)value),
            _ => throw new InvalidOperationException($"Unknown saveable key kind '{kind}'."),
        };
        return boxed ?? throw new InvalidOperationException(
            $"Could not box saveable key kind '{kind}'.");
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SaveableKeySnapshot other)
            return false;

        if (kind == JavaPeer || other.kind == JavaPeer)
            return JavaEquals(other);
        if (kind != other.kind)
            return false;

        return kind switch
        {
            Float => FloatBits((float)value) == FloatBits((float)other.value),
            Double => DoubleBits((double)value) == DoubleBits((double)other.value),
            _ => value.Equals(other.value),
        };
    }

    public override int GetHashCode() => kind switch
    {
        JavaPeer => ((Java.Lang.Object)value).GetHashCode(),
        String => JavaStringHash((string)value),
        Boolean => (bool)value ? 1231 : 1237,
        Character => (char)value,
        Byte => (sbyte)value,
        Short => (short)value,
        Integer => (int)value,
        Long => unchecked((int)((long)value ^ ((long)value >> 32))),
        Float => FloatBits((float)value),
        Double => unchecked((int)(DoubleBits((double)value) ^ (DoubleBits((double)value) >> 32))),
        _ => throw new InvalidOperationException($"Unknown saveable key kind '{kind}'."),
    };

    bool JavaEquals(SaveableKeySnapshot other)
    {
        var left = Box(out var ownsLeft);
        try
        {
            var right = other.Box(out var ownsRight);
            try
            {
                return left.Equals(right);
            }
            finally
            {
                if (ownsRight)
                    right.Dispose();
            }
        }
        finally
        {
            if (ownsLeft)
                left.Dispose();
        }
    }

    static int FloatBits(float value) =>
        float.IsNaN(value) ? 0x7fc00000 : BitConverter.SingleToInt32Bits(value);

    static long DoubleBits(double value) =>
        double.IsNaN(value) ? 0x7ff8000000000000L : BitConverter.DoubleToInt64Bits(value);

    static int JavaStringHash(string value)
    {
        int hash = 0;
        foreach (char character in value)
            hash = unchecked(31 * hash + character);
        return hash;
    }
}
