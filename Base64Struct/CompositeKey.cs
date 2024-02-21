using Microsoft.AspNetCore.WebUtilities;

namespace Base64Struct;

public class CompositeKey
{
    // RHS of shift operators must be an int or long and only the least significant 5 (int) or 6 (long) bits are used.
    // see: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/bitwise-and-shift-operators#shift-count-of-the-shift-operators
    private const int DistributionShift = 48;
    private const long RecordMask = 0x0000ffffffffffff;
    private short _distribution = 0;
    private long _record = 0;

    public short Distribution
    {
        get => _distribution;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Distribution value must not be negative: {value}");
            _distribution = value;
        }
    }

    public long Record
    {
        get => _record;
        set
        {
            if ((value & (~RecordMask)) != 0)
                throw new ArgumentOutOfRangeException(
                    $"Record value must not exceed 48 least signficant bits: {value}");
            _record = value;
        }
    }
    
    protected internal byte[] ToBytes()
    {
        long result = (long)Distribution << DistributionShift;
        result += Record & RecordMask;
        return BitConverter.GetBytes(result);
    }

    protected internal static CompositeKey FromBytes(byte[] bytes)
    {
        long result = BitConverter.ToInt64(bytes, 0);
        var record = result & RecordMask;
        var distribution = (short)(result >>> DistributionShift);
        return new CompositeKey { Distribution = distribution, Record = record };
    }

    public string ToBase64Url()
    {
        return WebEncoders.Base64UrlEncode(ToBytes());
    }

    public static CompositeKey FromBase64Url(string encoded)
    {
        return FromBytes(WebEncoders.Base64UrlDecode(encoded));
    }
}

#if DEBUG

public class CompositeKeyTests
{
    [Theory]
    [InlineData(0, long.MinValue)]
    [InlineData(0, long.MaxValue)]
    [InlineData(0, -1)]
    [InlineData(0, 0x0001ffffffffffff)]
    public void Cord_record_values_exceeding_least_significant_48bits_throw_ArgumentOutOfRangeException(
        short distribution,
        long record)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompositeKey
        {
            Distribution = distribution,
            Record = record
        });
    }
    
    [Theory]
    [InlineData(short.MinValue, 0)]
    [InlineData(-1, 0x0000ffffffffffff)]
    public void Cord_negative_partition_values_throw_ArgumentOutOfRangeException(short distribution, long record)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompositeKey
        {
            Distribution = distribution,
            Record = record
        });
    }
    
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(short.MaxValue, 0x0000ffffffffffff)]
    public void Cord_roundtrip_thru_byte_array_produces_equivalent_values(short distribution, long record)
    {
        var expected = new CompositeKey { Distribution = distribution, Record = record };
        var bytes = expected.ToBytes();
        var actual = CompositeKey.FromBytes(bytes);
        Assert.Equal(expected.Record, actual.Record);
        Assert.Equal(expected.Distribution, actual.Distribution);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(short.MaxValue, 0x0000ffffffffffff)]
    public void Cord_base64url_encoding_roundtrip_produces_equivalent_value(short distribution, long record)
    {
        var expected = new CompositeKey { Distribution = distribution, Record = record };
        var encoded = expected.ToBase64Url();
        var actual = CompositeKey.FromBase64Url(encoded);
        Assert.Equal(expected.Record, actual.Record);
        Assert.Equal(expected.Distribution, actual.Distribution);
    }
}

#endif