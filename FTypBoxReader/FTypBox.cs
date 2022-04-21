using System.Text;

namespace FTypBoxReader
{
    public class FTypBox
    {
        private List<string> _compatibleBrands = new List<string>();
        private FTypBox(string type, UInt32 size, Memory<byte> buf)
        {
            Type = type;
            Size = size;
            MajorBrand = Encoding.ASCII.GetString(buf.Slice(0, 4).ToArray());
            var versionbuf = buf.Slice(4,4).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(versionbuf);
            MinorVersion = BitConverter.ToUInt32(versionbuf);
            for (int i = 8; i < size; i += 4)
            {
                _compatibleBrands.Add(Encoding.ASCII.GetString(buf.Slice(i, 4).ToArray()));
            }
        }
        public string MajorBrand { get; }
        public UInt32 MinorVersion { get; }
        public IEnumerable<string> CompatibleBrands => _compatibleBrands;


        public string Type { get; }
        public uint Size { get; }

        private const int FTYP_HEADER_SIZE= 8;
        public static async Task<FTypBox?> TryReadFTypBoxAsync(Stream s)
        {
            int k = 0;
            Memory<byte> buf = new byte[FTYP_HEADER_SIZE];
            var rem = buf;
            while (k < FTYP_HEADER_SIZE)
                k += await s.ReadAsync(rem.Slice(k, FTYP_HEADER_SIZE - k));
                
            var size = buf.Slice(0, 4).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(size);
            }
            var ft = buf.Slice(4, 4);

            var typ = Encoding.ASCII.GetString(ft.ToArray());
            if (typ == "ftyp")
            {
                var remainingBoxSize = (int)BitConverter.ToUInt32(size.ToArray())- FTYP_HEADER_SIZE;
                Memory<byte> data = new byte[remainingBoxSize];
                k = 0;
                while (k < remainingBoxSize)
                {
                    k += await s.ReadAsync(data.Slice(k, remainingBoxSize - k));
                }
                return new FTypBox(typ, (UInt32)remainingBoxSize, data);
            }
            else return null;
        }
    }
}