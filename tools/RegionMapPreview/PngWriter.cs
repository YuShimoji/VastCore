using System;
using System.IO;
using System.IO.Compression;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>RGBA32 の画素を PNG に書く最小の実装（確認用コンソール専用）。</summary>
    internal static class PngWriter
    {
        private static readonly uint[] CrcTable = BuildCrcTable();

        public static void Write(string path, byte[] rgba, int width, int height)
        {
            using (var file = File.Create(path))
            {
                file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);

                var header = new byte[13];
                WriteInt(header, 0, width);
                WriteInt(header, 4, height);
                header[8] = 8;  // 1 チャンネル 8 bit
                header[9] = 6;  // RGBA
                WriteChunk(file, "IHDR", header, header.Length);

                using (var packed = new MemoryStream())
                {
                    using (var zlib = new ZLibStream(packed, CompressionLevel.Optimal, true))
                    {
                        var filter = new byte[] { 0 };
                        int stride = width * 4;
                        for (int y = 0; y < height; y++)
                        {
                            zlib.Write(filter, 0, 1);
                            zlib.Write(rgba, y * stride, stride);
                        }
                    }
                    WriteChunk(file, "IDAT", packed.GetBuffer(), (int)packed.Length);
                }

                WriteChunk(file, "IEND", new byte[0], 0);
            }
        }

        private static void WriteChunk(Stream stream, string type, byte[] data, int length)
        {
            var head = new byte[8];
            WriteInt(head, 0, length);
            for (int i = 0; i < 4; i++) head[4 + i] = (byte)type[i];
            stream.Write(head, 0, 8);
            stream.Write(data, 0, length);
            uint crc = 0xFFFFFFFFu;
            for (int i = 4; i < 8; i++) crc = CrcTable[(crc ^ head[i]) & 0xFF] ^ (crc >> 8);
            for (int i = 0; i < length; i++) crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            var tail = new byte[4];
            WriteInt(tail, 0, unchecked((int)(crc ^ 0xFFFFFFFFu)));
            stream.Write(tail, 0, 4);
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }
    }
}
