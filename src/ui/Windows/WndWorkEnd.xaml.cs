using System.Windows;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndWorkEnd.xaml
    /// </summary>
    public partial class WndWorkEnd : Window
    {
        public WndWorkEnd() {
            InitializeComponent();

            this.btOne.Click += this.BtOne_Click;
        }

        public void DoConfirm(Window owner = null) {
            this.Owner = owner;
            this.ShowDialog();
        }

        private void BtOne_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
