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

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for PgRecipe.xaml
    /// </summary>
    public partial class PgRecipe : Page
    {
        public PgRecipe()
        {
            InitializeComponent();
            this.Loaded += PgRecipe_Loaded;
            this.Unloaded += PgRecipe_Unloaded;
            this.btSaveRecipe.Click += BtSaveRecipe_Click;
        }

        private void BtSaveRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (this.cbModel.SelectedValue != null)
            {
                UiManager.appSettings.connection.model = this.cbModel.SelectedValue.ToString();
            }
            UiManager.SaveAppSettings();
        }

        private void PgRecipe_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void PgRecipe_Loaded(object sender, RoutedEventArgs e)
        {
            this.cbModel.SelectedValue = UiManager.appSettings.connection.model;
        }
    }
}
