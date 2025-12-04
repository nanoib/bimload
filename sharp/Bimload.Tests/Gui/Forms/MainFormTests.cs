using System.Windows.Forms;
using Bimload.Gui.Forms;
using FluentAssertions;
using Xunit;

namespace Bimload.Tests.Gui.Forms;

public class MainFormTests
{
    [Fact]
    public void MainForm_Should_Create_Without_Exception()
    {
        // Arrange & Act
        MainForm? form = null;
        Exception? exception = null;

        try
        {
            form = new MainForm();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().BeNull("форма должна создаваться без исключений");
        form.Should().NotBeNull("форма должна быть создана");
        form!.Visible.Should().BeTrue("форма должна быть видимой");
        form.WindowState.Should().Be(FormWindowState.Normal, "форма должна быть в нормальном состоянии");
    }

    [Fact]
    public void MainForm_Should_Have_Correct_Minimum_Size()
    {
        // Arrange
        var form = new MainForm();

        // Act & Assert
        // New minimum height: DataGridView (300) + spacing (25) + buttons (45) + spacing (25) + status (28) + padding (35) = 458px
        form.MinimumSize.Height.Should().BeGreaterThanOrEqualTo(400, "минимальная высота формы должна быть не менее 400px");
        form.MinimumSize.Width.Should().BeGreaterThan(0, "минимальная ширина формы должна быть больше 0");
    }
}

