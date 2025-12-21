using Algora.Infrastructure.Services.Scrapers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Algora.Tests.Scrapers;

public class AmazonReviewScraperTests
{
    private readonly AmazonReviewScraper _scraper;

    public AmazonReviewScraperTests()
    {
        var loggerMock = new Mock<ILogger<AmazonReviewScraper>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var options = Options.Create(new ScraperApiOptions
        {
            Enabled = false,
            ApiKey = ""
        });

        httpClientFactoryMock
            .Setup(f => f.CreateClient("ReviewScraper"))
            .Returns(new HttpClient());

        _scraper = new AmazonReviewScraper(loggerMock.Object, httpClientFactoryMock.Object, options);
    }

    [Theory]
    [InlineData("https://www.amazon.com/dp/B09V3KXJPB", true)]
    [InlineData("https://www.amazon.com/product/B09V3KXJPB", true)]
    [InlineData("https://www.amazon.com/gp/product/B09V3KXJPB", true)]
    [InlineData("https://www.amazon.com/Some-Product-Name/dp/B09V3KXJPB/ref=sr_1_1", true)]
    [InlineData("https://amazon.com/dp/B09V3KXJPB", true)]
    [InlineData("https://www.amazon.co.uk/dp/B09V3KXJPB", true)]
    [InlineData("https://www.amazon.de/dp/B09V3KXJPB", true)]
    [InlineData("https://www.aliexpress.com/item/1005006789012.html", false)]
    [InlineData("https://www.ebay.com/itm/12345", false)]
    [InlineData("", false)]
    [InlineData("not-a-url", false)]
    public void CanHandle_ShouldCorrectlyIdentifyAmazonUrls(string url, bool expected)
    {
        var result = _scraper.CanHandle(url);
        result.Should().Be(expected);
    }

    [Fact]
    public void SourceType_ShouldBeAmazon()
    {
        _scraper.SourceType.Should().Be("amazon");
    }
}
