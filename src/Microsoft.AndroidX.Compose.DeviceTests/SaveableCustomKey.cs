namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class SaveableCustomKey(int equalityValue, string marshalledValue)
{
    readonly int equalityValue = equalityValue;
    string marshalledValue = marshalledValue;

    internal int ToStringCalls { get; private set; }

    internal string MarshalledValue
    {
        get => marshalledValue;
        set => marshalledValue = value;
    }

    public override bool Equals(object? obj) =>
        obj is SaveableCustomKey other && equalityValue == other.equalityValue;

    public override int GetHashCode() => equalityValue;

    public override string ToString()
    {
        ToStringCalls++;
        return marshalledValue;
    }
}
