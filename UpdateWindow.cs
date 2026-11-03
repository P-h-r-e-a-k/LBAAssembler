using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace LBAAssembler;

// A newer LBA Assembler found (EditorUpdater): update now (the editor closes as it always does, asking about anything unsaved, and the new one starts), later (asked again at
// the next start), or skip this one. Built in code, as ReportWindow is.
internal sealed class UpdateWindow : Window
{
    public enum Answer { Later, Now, Skip }
    public Answer Choice { get; private set; } = Answer.Later;

    public UpdateWindow(EditorUpdater.Release release)
    {
        Title = "LBA Assembler update";
        Width = 440; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(Control.BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(Control.ForegroundProperty, "ThemeTextBrush");
        ShowInTaskbar = false;

        var panel = new StackPanel { Margin = new Thickness(14) };
        var canSwap = release.NewFile is not null;
        var said = new TextBlock
        {
            Text = canSwap
                ? $"A new LBA Assembler is out: {release.Tag}.\n\nUpdate now closes the editor, asking about anything unsaved as it always does, and opens the new one."
                : $"A new LBA Assembler is out: {release.Tag}.\n\nIt can't be put in this editor's folder, which can't be written to here: download it from its page instead.",
            TextWrapping = TextWrapping.Wrap,
        };
        said.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        panel.Children.Add(said);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        Button Add(string text, string id, Answer answer, bool primary = false)
        {
            var b = new Button { Content = text, Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(8, 0, 0, 0), IsDefault = primary, IsCancel = answer == Answer.Later };
            AutomationProperties.SetAutomationId(b, id);
            b.Click += (_, _) => { Choice = answer; Close(); };
            buttons.Children.Add(b);
            return b;
        }
        Add(canSwap ? "Update now" : "Open its page", "UpdateNow", Answer.Now, primary: true);
        Add("Later", "UpdateLater", Answer.Later);
        Add("Skip this version", "UpdateSkip", Answer.Skip);
        panel.Children.Add(buttons);
        Content = panel;
    }
}
