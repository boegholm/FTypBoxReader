using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FTypBoxReader.Tests.Properties;
using Xunit;

namespace FTypBoxReader.Tests
{
    static class ByteArrayExtensions
    {
        enum Endianness { BigEndian, LittleEndian};
        public static byte[] ReplaceBigEndianUInt32(this byte[] buf, int index, UInt32 newVal)
        {
            var valbuf = BitConverter.GetBytes(newVal);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(valbuf);
            Array.ConstrainedCopy(valbuf, 0, buf, index, valbuf.Length);
            return buf;
        }
        public static byte[] ReplaceLittleEndianUInt32(this byte[] buf, int index, UInt32 newVal)
        {
            var valbuf = BitConverter.GetBytes(newVal);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(valbuf);
            Array.ConstrainedCopy(valbuf, 0, buf, index, valbuf.Length);
            return buf;
        }
    }
    // Test images from https://github.com/tigranbs/test-heic-images
    public class FTypBoxTestsu
    {
        public static TheoryData<Stream> HeicSources => new()
        {
            { new MemoryStream(Resources.image1) },
            { new MemoryStream(Resources.image2) },
            { new MemoryStream(Resources.image3) },
            { new MemoryStream(Resources.image4) },
        };

        public static TheoryData<Stream> MisalignedImages
            => new()
        {
            { MisalignedSize(Resources.image4, -1) },
            { MisalignedSize(Resources.image4, -2) },
            { MisalignedSize(Resources.image4, -3) },
            { MisalignedSize(Resources.image4, 1) },
            { MisalignedSize(Resources.image4, 2) },
            { MisalignedSize(Resources.image4, 3) }
        };
        private static MemoryStream MisalignedSize(byte[] image, int offset)
        {
            var bs = FTypBox.ReadBoxSize(image);
            return new MemoryStream(image.ReplaceBigEndianUInt32(0, (UInt32)(bs + offset)));
        }

        public static TheoryData<Stream> PngSources => new()
        {
            { new MemoryStream(Resources.image5) },
        };

        [Theory]
        [MemberData(nameof(HeicSources))]
        [MemberData(nameof(MisalignedImages))]
        public async Task TryReadFTypBoxAsync_LoadHeicPrimarilyHeic(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.Equal("heic",box?.MajorBrand);
        }


        [Theory]
        [MemberData(nameof(HeicSources))]
        public async Task TryReadFTypBoxAsync_CompatibleBrandsContainsTwo(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.Equal(2, box?.CompatibleBrands.Count());
        }

        

        [Theory]
        [MemberData(nameof(HeicSources))]
        public async Task TryReadFTypBoxAsync_CompatibleBrandsContainsMif1(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.True(box?.CompatibleBrands.Any(v => v.Equals("mif1"))); ;
        }
        [Theory]
        [MemberData(nameof(HeicSources))]
        [MemberData(nameof(MisalignedImages))]
        public async Task TestHeicFTypBoxLoad_Success(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.NotNull(box);
        }

        [Fact]
        public async Task LoadHugeSizeFTypBoxSizeImage_TruncatedSuccess()
        {
            var s = new MemoryStream(Resources.image4.ReplaceBigEndianUInt32(0, UInt32.MaxValue));
            var box = await LoadAndClose(s);
            Assert.NotNull(box);
        }

        [Fact]
        public async Task LoadZeroFTypBoxSizeImage_Fails()
        {
            var box = await LoadAndClose(new MemoryStream(Resources.image1.ReplaceBigEndianUInt32(0, 0)));
            Assert.Null(box);
        }

        [Theory]
        [MemberData(nameof(PngSources))]
        public async Task TestHeicFTypBoxLoad_LoadUnsuccessful(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.Null(box);
        }

        private async Task<FTypBox?> LoadAndClose(Stream s)
        {
            try
            {
                return await FTypBox.TryReadFTypBoxAsync(s);
            }
            finally
            {
                if (s != null)
                    await s.DisposeAsync();
            }

        }
    }
}