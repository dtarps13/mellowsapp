using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MellowsApp2
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string passwordEntered = PasswordBox.Password;
            // No auth, using environment variable
            string? mellows = Environment.GetEnvironmentVariable("MellowsLogin");

            if (mellows != null)
                if (mellows == passwordEntered)
                {
                    this.Content = new Members();
                }
                else
                {
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                    MessageBox.Show("Incorrect Password Entered");
                }
        }
        // Login button disabled until password box has content
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = !string.IsNullOrEmpty(PasswordBox.Password);
        }
    }
}
