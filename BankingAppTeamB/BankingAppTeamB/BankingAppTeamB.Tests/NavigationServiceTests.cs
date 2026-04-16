using BankingAppTeamB.Services;
using FluentAssertions;
using System;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class NavigationServiceTests
    {
        private void ResetFrame() => NavigationService.Frame = null;

        [Fact]
        public void Frame_IsNullByDefault()
        {
            ResetFrame();

            NavigationService.Frame.Should().BeNull();
        }

        [Fact]
        public void NavigateTo_DoesNotThrow_WhenFrameIsNull()
        {
            ResetFrame();

            Action act = () => NavigationService.NavigateTo<object>();

            act.Should().NotThrow();
        }

        [Fact]
        public void NavigateTo_WithParameter_DoesNotThrow_WhenFrameIsNull()
        {
            ResetFrame();

            Action act = () => NavigationService.NavigateTo<object>(parameter: "some-param");

            act.Should().NotThrow();
        }

        [Fact]
        public void GoBack_DoesNotThrow_WhenFrameIsNull()
        {
            ResetFrame();

            Action act = () => NavigationService.GoBack();

            act.Should().NotThrow();
        }

        [Fact]
        public void Frame_CanBeSet_AndRetrieved()
        {
            ResetFrame();
            NavigationService.Frame = null;
            NavigationService.Frame.Should().BeNull();
        }
    }
}
