using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

public partial class MainWindow : Window
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private string _avatarPath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectAvatar_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _avatarPath = openFileDialog.FileName;
            RegAvatar.Source = new BitmapImage(new Uri(_avatarPath));
        }
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (RegPassword.Password != RegPasswordConfirm.Password)
        {
            MessageBox.Show("Пароли не совпадают!");
            return;
        }

        if (string.IsNullOrWhiteSpace(RegEmail.Text) || RegPassword.Password.Length < 6)
        {
            MessageBox.Show("Email и пароль обязательны. Пароль должен быть не менее 6 символов.");
            return;
        }

        if (_dbService.RegisterUser(RegEmail.Text, RegPassword.Password, _avatarPath))
        {
            MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
            LoginEmail.Text = RegEmail.Text;
            LoginPassword.Password = RegPassword.Password;
        }
        else
        {
            MessageBox.Show("Пользователь с таким email уже существует.");
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var user = _dbService.AuthenticateUser(LoginEmail.Text, LoginPassword.Password);

        if (user == null)
        {
            MessageBox.Show("Неверный email или пароль.");
            return;
        }

        // Проверяем, заполнены ли основные данные
        if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
        {
            var accountWindow = new AccountWindow(user);
            accountWindow.Show();
        }
        else
        {
            var managerWin = new ManagerWin(user);
            managerWin.Show();
        }

        this.Close();
    }
}