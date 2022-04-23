using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FTypBoxReader.Tests.Properties;
using Xunit;

namespace FTypBoxReader.Tests
{
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
        
        public static TheoryData<Stream> MaliciousHeicImages => new()
        {
            { new MemoryStream(Resources.image1) },
            { new MemoryStream(Resources.image2) },
            { new MemoryStream(Resources.image3) },
            { new MemoryStream(Resources.image4) },
        };

        public static TheoryData<Stream> PngSources => new()
        {
            { new MemoryStream(Resources.image5) },
        };

        [Theory]
        [MemberData(nameof(HeicSources))]
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
        public async Task TestHeicFTypBoxLoad_Success(Stream s)
        {
            var box = await LoadAndClose(s);
            Assert.NotNull(box);
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