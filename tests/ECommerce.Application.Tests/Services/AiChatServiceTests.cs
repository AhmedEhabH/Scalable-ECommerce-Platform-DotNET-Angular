using System.Net;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace ECommerce.Application.Tests.Services;

public class AiChatServiceTests
{
    private const string FallbackMessage = "I'm currently experiencing technical difficulties. Please try again in a moment, or browse our products directly.";

    [Fact]
    public async Task GetChatResponseAsync_ShouldReturnFallback_WhenGeminiReturns429()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Too Many Requests")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Gemini:ApiKey"]).Returns("test-api-key");
        configMock.Setup(c => c["Ai:Provider"]).Returns("gemini");
        configMock.Setup(c => c["Gemini:Endpoint"]).Returns("https://example.com/model:generateContent");

        var loggerMock = new Mock<ILogger<AiChatService>>();

        var productRepoMock = new Mock<IProductRepository>();
        productRepoMock
            .Setup(r => r.GetActiveProductsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var service = new AiChatService(httpClient, configMock.Object, loggerMock.Object, productRepoMock.Object);

        var result = await service.GetChatResponseAsync("Hello", CancellationToken.None);

        result.Should().Be(FallbackMessage);
    }
}
