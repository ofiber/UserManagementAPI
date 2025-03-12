using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace UserManagementAPI.Tests
{
    public class ProgramTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ProgramTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOk()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            mockUserService.Setup(service => service.GetAllUsers()).Returns(GetTestUsers());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockUserService.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/users");

            // Assert
            response.EnsureSuccessStatusCode();
            var users = await response.Content.ReadFromJsonAsync<IEnumerable<User>>();
            Assert.NotNull(users);
            Assert.NotEmpty(users);
        }

        [Fact]
        public async Task GetUserById_ReturnsOk()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            mockUserService.Setup(service => service.GetUserById(1)).Returns(GetTestUser());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockUserService.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/users/1");

            // Assert
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<User>();
            Assert.NotNull(user);
            Assert.Equal(1, user.Id);
        }

        [Fact]
        public async Task CreateUser_ReturnsCreated()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            var newUser = new User(0, "New User", "newuser@example.com");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockUserService.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/users", newUser);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();
            var updatedUser = new User(1, "Updated User", "updateduser@example.com");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockUserService.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.PutAsJsonAsync("/users/1", updatedUser);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task DeleteUser_ReturnsNoContent()
        {
            // Arrange
            var mockUserService = new Mock<IUserService>();

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockUserService.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.DeleteAsync("/users/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static List<User> GetTestUsers()
        {
            return new()
            {
                new(1, "Test User 1", "test1@example.com"),
                new(2, "Test User 2", "test2@example.com")
            };
        }

        private static User GetTestUser()
        {
            return new(1, "Test User 1", "test1@example.com");
        }
    }
}