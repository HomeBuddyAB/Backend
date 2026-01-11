using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.DTOs.Requests.User;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Interfaces.UserInterfaces;
using HomeBuddy_API.Models;
using HomeBuddy_API.Services;
using Moq;
using Xunit;
using AdminUpdateUserDto = HomeBuddy_API.DTOs.Requests.AdminDashDTOs.UpdateUserDto;
using UserUpdateDto = HomeBuddy_API.DTOs.Requests.User.UserUpdateDto;

namespace HomeBuddy_API.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repoMock;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _service = new UserService(_repoMock.Object);
        }

        // Admin Functions Tests

        [Fact]
        // Tests that GetAllUsersAsync returns a list of users
        public async Task GetAllUsersAsync_ShouldReturnUsers()
        {
            var users = new List<UserResponse>
            {
                new UserResponse { Id = 1, Email = "test@example.com" }
            };
            _repoMock.Setup(r => r.GetAllAsync(It.IsAny<int>())).ReturnsAsync(users);

            var result = await _service.GetAllUsersAsync(1);

            Assert.Single(result);
            Assert.Equal("test@example.com", ((List<UserResponse>)result)[0].Email);
        }

        [Fact]
        // Tests that GetUserCountAsync returns the correct count
        public async Task GetUserCountAsync_ShouldReturnCount()
        {
            _repoMock.Setup(r => r.GetUserCountAsync()).ReturnsAsync(50);

            var result = await _service.GetUserCountAsync();

            Assert.Equal(50, result);
        }

        [Fact]
        // Tests that GetUserByIdAsync returns a user if it exists
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
        {
            var user = new UserResponse { Id = 1, Email = "user@example.com" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.GetUserByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("user@example.com", result.Email);
        }

        [Fact]
        // Tests that UpdateUserAsync correctly updates email
        public async Task UpdateUserAsync_ShouldUpdateEmail_WhenProvided()
        {
            var existingUser = new User
            {
                Email = "old@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            existingUser.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(existingUser);

            var dto = new AdminUpdateUserDto { Email = "new@example.com" };
            await _service.UpdateUserAsync(1, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
                u.Id == 1 &&
                u.Email == "new@example.com"
            )), Times.Once);
        }

        [Fact]
        // Tests that UpdateUserAsync correctly updates password
        public async Task UpdateUserAsync_ShouldUpdatePassword_WhenProvided()
        {
            var existingUser = new User
            {
                Email = "user@example.com",
                PasswordHash = "oldHash",
                PasswordSalt = "oldSalt"
            };
            existingUser.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(existingUser);

            var dto = new AdminUpdateUserDto { NewPassword = "newPassword123" };
            await _service.UpdateUserAsync(1, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
                u.Id == 1 &&
                u.PasswordHash != "oldHash" &&
                u.PasswordSalt != "oldSalt"
            )), Times.Once);
        }

        [Fact]
        // Tests that UpdateUserAsync throws if user does not exist
        public async Task UpdateUserAsync_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync((User?)null);
            var dto = new AdminUpdateUserDto { Email = "new@example.com" };

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateUserAsync(1, dto));
        }

        [Fact]
        // Tests that DeleteUserAsync calls the repository delete method if user exists
        public async Task DeleteUserAsync_ShouldCallRepoDelete_WhenUserExists()
        {
            var existingUser = new User
            {
                Email = "user@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            existingUser.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(existingUser);

            await _service.DeleteUserAsync(1);

            _repoMock.Verify(r => r.DeleteAsync(existingUser), Times.Once);
        }

        [Fact]
        // Tests that DeleteUserAsync throws if user does not exist
        public async Task DeleteUserAsync_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<Exception>(() => _service.DeleteUserAsync(1));
        }

        // Profile Functions Tests

        [Fact]
        // Tests that GetOwnProfileAsync returns the user's profile
        public async Task GetOwnProfileAsync_ShouldReturnUser()
        {
            var user = new User
            {
                Email = "user@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            user.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(user);

            var result = await _service.GetOwnProfileAsync(1);

            Assert.NotNull(result);
            Assert.Equal("user@example.com", result.Email);
        }

        [Fact]
        // Tests that UpdateOwnProfileAsync updates email
        public async Task UpdateOwnProfileAsync_ShouldUpdateEmail()
        {
            var user = new User
            {
                Email = "old@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            user.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(user);

            var dto = new UserUpdateDto { Email = "new@example.com" };
            await _service.UpdateOwnProfileAsync(1, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
                u.Id == 1 &&
                u.Email == "new@example.com"
            )), Times.Once);
        }

        [Fact]
        // Tests that UpdateOwnProfileAsync throws if user does not exist
        public async Task UpdateOwnProfileAsync_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync((User?)null);
            var dto = new UserUpdateDto { Email = "new@example.com" };

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateOwnProfileAsync(1, dto));
        }

        [Fact]
        // Tests that UpdateOwnProfileAsync throws on incorrect current password
        public async Task UpdateOwnProfileAsync_ShouldThrow_WhenCurrentPasswordIncorrect()
        {
            // Use valid Base-64 encoded strings for hash and salt
            var user = new User
            {
                Email = "user@example.com",
                PasswordHash = "aGFzaA==",
                PasswordSalt = "c2FsdA=="
            };
            user.Id = 1;
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(user);

            var dto = new UserUpdateDto
            {
                Email = "user@example.com",
                CurrentPassword = "wrongPassword",
                NewPassword = "newPassword123"
            };

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateOwnProfileAsync(1, dto));
        }

        [Fact]
        // Tests that DeleteOwnAccountAsync deletes user with correct password
        public async Task DeleteOwnAccountAsync_ShouldThrow_WhenPasswordIncorrect()
        {
            // Create a user with valid Base-64 encoded hash and salt
            var user = new User
            {
                Email = "user@example.com",
                PasswordHash = "aGFzaA==",
                PasswordSalt = "c2FsdA=="
            };
            user.Id = 1;

            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync(user);

            var dto = new UserDeleteDto { Password = "wrongPassword" };

            // This will throw because password verification will fail
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteOwnAccountAsync(1, dto));
        }

        [Fact]
        // Tests that DeleteOwnAccountAsync throws if user does not exist
        public async Task DeleteOwnAccountAsync_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetByIdFullAsync(1)).ReturnsAsync((User?)null);
            var dto = new UserDeleteDto { Password = "password" };

            await Assert.ThrowsAsync<Exception>(() => _service.DeleteOwnAccountAsync(1, dto));
        }
    }
}