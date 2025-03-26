using System.Windows;
using LINQPad.Extensibility.DataContext;

namespace Linq2MongoDB.LINQPadDriver
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
    {
        private readonly IConnectionInfo _cxInfo;

        public ConnectionDialog(IConnectionInfo cxInfo)
        {
            if (string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.CustomCxString))
            {
                cxInfo.DatabaseInfo.CustomCxString = "mongodb://mongo:27017";
            }
            _cxInfo = cxInfo;
            DataContext = cxInfo;
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cxInfo.DatabaseInfo.Database))
            {
                MessageBox.Show("Database must be specified");
                DialogResult = null;
                return;
            }
            DialogResult = true;
        }
    }
}
