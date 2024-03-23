
namespace MNet.Helpers;

public static class RandomUtils {

    public static string CreateWebsocketBase64Key() {

        return RandomString(16);

    }

    public static string RandomString(int length) {

        Span<byte> buffer = length > 1028 
            ? new byte[length] : stackalloc byte[length];

        RandomBytes(ref buffer);

        return Convert.ToBase64String(buffer);

    }

    public static int Next(int min, int maxExclusive) {

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, maxExclusive);

        long diff = (long)maxExclusive - min;
        long upperBound = uint.MaxValue / diff * diff;

        uint ui;

        do {

            ui = RandomUInt();

        } while (ui >= upperBound);

        return (int)(min + (ui % diff));

    }

    public static uint RandomUInt() {

        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        RandomBytes(ref buffer);

        return BinaryPrimitives.ReadUInt32BigEndian(buffer);

    }

    public static void RandomBytes(ref Span<byte> buffer) {

        RandomNumberGenerator.Fill(buffer);

    }

}

