using System.Net.Http;
using System.Threading.Tasks;
using Dka.AspNetCore.BasicWebApp.Controllers;
using Dka.AspNetCore.BasicWebApp.Models.ApiClients;
using Dka.AspNetCore.BasicWebApp.Models.Constants;
using Dka.AspNetCore.BasicWebApp.Services.ApiClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dka.AspNetCore.BasicWebApp.UnitTests.Controllers
{
    public class AboutControllerTests
    {
        private (AboutController, Mock<IInternalApiClient>) SetupController()
        {
            var logger = new Mock<ILogger<AboutController>>();
            var internalApiClient = new Mock<IInternalApiClient>();
            var aboutController = new AboutController(internalApiClient.Object, logger.Object);

            return (aboutController, internalApiClient);
        }
        
        private (AboutController, Mock<IInternalApiClient>) SetupControllerWithThrowingException()
        {
            var logger = new Mock<ILogger<AboutController>>();
            var internalApiClient = new Mock<IInternalApiClient>();
            var aboutController = new AboutController(internalApiClient.Object, logger.Object);

            return (aboutController, internalApiClient);
        }

        [Fact]
        public void TestingIndexAction_ShouldPass()
        {
            var (aboutController, internalApiClient) = SetupController();

            var result = aboutController.Index();

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("About", viewResult.ViewData[ViewDataKeys.HtmlPageNameReceivedFromApi]);
        }
    }
}