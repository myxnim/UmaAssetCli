namespace UmaAsset.Game.Services;

public sealed class EncryptedAssetStream : FileStream
{
    private const int HeaderSize = 256;

    private static readonly byte[] BaseKeys =
    {
        0x53, 0x2B, 0x46, 0x31, 0xE4, 0xA7, 0xB9, 0x47, 0x3E, 0x7C, 0xFB,
    };

    private readonly byte[] keys;

    public EncryptedAssetStream(string fileName, long key)
        : base(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    {
        keys = new byte[BaseKeys.Length * 8];
        var keyBytes = BitConverter.GetBytes(key);

        for (var i = 0; i < BaseKeys.Length; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                var index = j + (i * 8);
                keys[index] = (byte)(BaseKeys[i] ^ keyBytes[j]);
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = base.Read(buffer, offset, count);
        if (read <= 0)
        {
            return read;
        }

        DecryptBuffer(buffer.AsSpan(offset, read), Position - read);

        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = base.Read(buffer);
        if (read <= 0)
        {
            return read;
        }

        DecryptBuffer(buffer[..read], Position - read);
        return read;
    }

    private void DecryptBuffer(Span<byte> buffer, long absoluteStartPosition)
    {
        for (var index = 0; index < buffer.Length; index++)
        {
            var absolutePosition = absoluteStartPosition + index;
            if (absolutePosition < HeaderSize)
            {
                continue;
            }

            buffer[index] = (byte)(buffer[index] ^ keys[absolutePosition % keys.Length]);
        }
    }
}
