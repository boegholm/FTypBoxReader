using Microsoft.Extensions.Logging;
using System.Text;

namespace FTypBoxReader
{
    public class FTypBox
    {
        private List<string> _compatibleBrands = new List<string>();
        private FTypBox(string type, int size, Memory<byte> buf)
        {
            Type = type;
            Size = size;
            MajorBrand = Encoding.ASCII.GetString(buf.Slice(0, 4).ToArray());
            var versionbuf = buf.Slice(4,4).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(versionbuf);
            MinorVersion = BitConverter.ToUInt32(versionbuf);
            for (int i = 8; i < size-FTYP_HEADER_SIZE; i += 4)
            {
                _compatibleBrands.Add(Encoding.ASCII.GetString(buf.Slice(i, 4).ToArray()));
            }
        }
        public string MajorBrand { get; }
        public UInt32 MinorVersion { get; }
        public IEnumerable<string> CompatibleBrands => _compatibleBrands;


        public string Type { get; }
        public int Size { get; }

        private const int FTYP_HEADER_SIZE= 8;
        private const UInt16 MAX_BOX_SIZE = 128;
        public static async Task<FTypBox?> TryReadFTypBoxAsync(Stream s, ILogger<FTypBox>? logger=null)
        {
            int k = 0;
            Memory<byte> buf = new byte[FTYP_HEADER_SIZE];
            var rem = buf;
            while (k < FTYP_HEADER_SIZE)
                k += await s.ReadAsync(rem.Slice(k, FTYP_HEADER_SIZE - k));

            uint boxSize = ReadBoxSize(buf);
            if (boxSize <= 16)
            {  // header+major+minor_version 
                logger?.LogCritical($"box probably too small to be ftyp (or 1/largesize -- and ignored): {boxSize}");
            }
            var ft = buf.Slice(4, 4);

            var typ = Encoding.ASCII.GetString(ft.ToArray());
            if (typ == "ftyp")
            {
                int truncatedBoxSize;
                if (boxSize <= 16)
                {  // header+major+minor_version 
                    logger?.LogCritical($"ftyp but box too small: ({boxSize})");
                    return null;
                }
                if (boxSize > MAX_BOX_SIZE)
                {
                    truncatedBoxSize = MAX_BOX_SIZE;
                }
                else
                {
                    truncatedBoxSize = (int)boxSize;
                }
                var remainingBoxSize = truncatedBoxSize - FTYP_HEADER_SIZE;
                Memory<byte> data = new byte[remainingBoxSize];
                k = 0;
                while (k < remainingBoxSize)
                {
                    k += await s.ReadAsync(data.Slice(k, remainingBoxSize - k));
                }
                return new FTypBox(typ, truncatedBoxSize, data);
            }
            else return null;
        }

        public static UInt32 ReadBoxSize(Memory<byte> buf, ILogger<FTypBox>? logger = null)
        {
            var size = buf.Slice(0, 4).ToArray();
            size[3] &= 0xFC;
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(size);
            }
            var boxSize = BitConverter.ToUInt32(size.ToArray()); 
            return boxSize; // fix alignment
        }
    }
}