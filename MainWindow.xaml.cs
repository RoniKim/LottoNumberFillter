using System.Windows;
using LottoNumber.ViewModels;

namespace LottoNumber
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
