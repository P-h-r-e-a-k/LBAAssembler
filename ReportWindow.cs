using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LBAAssembler;

// Help's Report a bug and Request a feature: a few words, sent to GitHub by IssueReport. It says it is public before anything goes. Built in code.
internal sealed class ReportWindow : Window
{
    private const int MostWords = 2000;
    private readonly TextBox words = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button send = new() { Content = "Send", Padding = new Thickness(18, 4, 18, 4), Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
    private readonly bool feature;

    public ReportWindow(bool feature)
    {
        this.feature = feature;
        Title = feature ? "Request a feature" : "Report a bug";
        Width = 520; Height = 380; MinWidth = 400; MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(Control.BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(Control.ForegroundProperty, "ThemeTextBrush");
        ShowInTaskbar = false;

        var panel = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(14, 4, 14, 4), IsCancel = true };
        buttons.Children.Add(send); buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Bottom);
        panel.Children.Add(buttons);

        status.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        DockPanel.SetDock(status, Dock.Bottom);
        panel.Children.Add(status);

        var ask = new TextBlock { Text = feature ? "What would you like LBA Assembler to do?" : "What went wrong, and what were you doing when it did?", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(ask, Dock.Top);
        panel.Children.Add(ask);
        var note = new TextBlock
        {
            Text = feature ? "This goes on our public GitHub page." : "This goes on our public GitHub page with the editor's log. Names, folders and addresses are taken out first.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8),
        };
        note.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextMutedBrush");
        DockPanel.SetDock(note, Dock.Top);
        panel.Children.Add(note);

        words.AcceptsReturn = true; words.TextWrapping = TextWrapping.Wrap; words.MaxLength = MostWords;
        words.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        words.Padding = new Thickness(4, 3, 4, 3);
        words.SetResourceReference(Control.BackgroundProperty, "ThemeFieldBrush");
        words.SetResourceReference(Control.ForegroundProperty, "ThemeTextBrush");
        words.TextChanged += (_, _) => send.IsEnabled = words.Text.Trim().Length > 0;
        panel.Children.Add(words);
        Content = panel;

        send.Click += async (_, _) => await Send();
        Loaded += (_, _) => Keyboard.Focus(words);
    }

    private async Task Send()
    {
        send.IsEnabled = false; words.IsEnabled = false;
        status.Text = "Sending ...";
        var problem = await IssueReport.Send(feature, words.Text);
        if (problem is null) { status.Text = "Sent. Thank you!"; await Task.Delay(1100); Close(); return; }
        status.Text = problem;
        words.IsEnabled = true; send.IsEnabled = true;
    }
}
