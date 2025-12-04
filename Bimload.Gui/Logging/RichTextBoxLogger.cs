using System.Drawing;
using System.Windows.Forms;
using Bimload.Core.Logging;

namespace Bimload.Gui.Logging;

public class RichTextBoxLogger : ILogger
{
    private readonly RichTextBox _richTextBox;

    public RichTextBoxLogger(RichTextBox richTextBox)
    {
        _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        if (_richTextBox.InvokeRequired)
        {
            _richTextBox.Invoke(new Action(() => LogInternal(message, level)));
        }
        else
        {
            LogInternal(message, level);
        }
    }

    private void LogInternal(string message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Ensure we have a valid font
        if (_richTextBox.Font == null)
        {
            _richTextBox.Font = new Font("Consolas", 9);
        }

        // Add timestamp in gray
        _richTextBox.SelectionStart = _richTextBox.TextLength;
        _richTextBox.SelectionLength = 0;
        _richTextBox.SelectionColor = Color.Gray;
        _richTextBox.SelectionFont = new Font(_richTextBox.Font.FontFamily, _richTextBox.Font.Size, FontStyle.Regular);
        _richTextBox.AppendText($"[{timestamp}] ");

        // Add message with appropriate color and style
        _richTextBox.SelectionStart = _richTextBox.TextLength;
        _richTextBox.SelectionLength = 0;

        Color messageColor;
        FontStyle messageStyle;

        switch (level)
        {
            case LogLevel.Info:
                messageColor = Color.Black;
                messageStyle = FontStyle.Regular;
                break;
            case LogLevel.Error:
                messageColor = Color.Red;
                messageStyle = FontStyle.Bold;
                break;
            case LogLevel.Warning:
                messageColor = Color.Orange;
                messageStyle = FontStyle.Bold;
                break;
            case LogLevel.Success:
                messageColor = Color.Green;
                messageStyle = FontStyle.Bold;
                break;
            default:
                messageColor = Color.Black;
                messageStyle = FontStyle.Regular;
                break;
        }

        _richTextBox.SelectionColor = messageColor;
        _richTextBox.SelectionFont = new Font(_richTextBox.Font.FontFamily, _richTextBox.Font.Size, messageStyle);
        _richTextBox.AppendText($"{message}{Environment.NewLine}");

        // Auto-scroll to bottom
        _richTextBox.SelectionStart = _richTextBox.TextLength;
        _richTextBox.ScrollToCaret();
    }
}

