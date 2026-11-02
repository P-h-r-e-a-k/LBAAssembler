using System.Windows;

namespace LBAAssembler;

// Help's Report a bug and Request a feature: ReportWindow
public partial class MainWindow
{
    private void ReportBug_Click(object sender, RoutedEventArgs e) => new ReportWindow(false) { Owner = this }.ShowDialog();

    private void RequestFeature_Click(object sender, RoutedEventArgs e) => new ReportWindow(true) { Owner = this }.ShowDialog();
}
