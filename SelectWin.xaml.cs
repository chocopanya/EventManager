using System.Windows;

public partial class SelectWin : Window
{
    private readonly User _currentUser;

    public SelectWin(User user)
    {
        InitializeComponent();
        _currentUser = user;
    }

    private void AccountButton_Click(object sender, RoutedEventArgs e)
    {
        var accountWindow = new AccountWindow(_currentUser);
        accountWindow.Show();
        this.Close();
    }

    private void BuyTicketButton_Click(object sender, RoutedEventArgs e)
    {
        var ticketsWindow = new Tickets(_currentUser);
        ticketsWindow.Show();
        this.Close();
    }

    private void MyEventsButton_Click(object sender, RoutedEventArgs e)
    {
        var myEventsWindow = new MyEventsWindow(_currentUser);
        myEventsWindow.Show();
        this.Close();
    }
}