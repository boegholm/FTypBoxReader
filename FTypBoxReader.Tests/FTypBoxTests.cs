using System.IO;
using System.Threading.Tasks;
using FTypBoxReader.Tests.Properties;
using Xunit;

namespace FTypBoxReader.Tests
{
    // Test images from https://github.com/tigranbs/test-heic-images
    public class FTypBoxTests
    {
          public static TheoryData<Stream> HeicSources => new()
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
        public async Task TestHeicFTypBoxLoad_Success(Stream s)
        {
            try
            {
                var box = await FTypBox.TryReadFTypBoxAsync(s);
                Assert.NotNull(box);
            }
            finally
            {
                if(s!=null)
                    await s.DisposeAsync();
            }
        }
    }
}