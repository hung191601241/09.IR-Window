using System;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections.Generic;


namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for PgIO.xaml
    /// </summary>
    public partial class PgIO : Page
    {
        private static MyLogger Logger = new MyLogger("PgIOCheck");

        private ManualResetEvent exitFlagInput = new ManualResetEvent(false);
        private ManualResetEvent exitFlagOutput = new ManualResetEvent(false);

        private List<IoPort> listPointInput1;
        private List<IoPort> listPointInput2;

        private List<IoPort> listPointOutput1;
        private List<IoPort> listPointOutput2;

        private bool isInputActive = true;

        private const int ROW_CNT = 17;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        private int _noModulNow;
        public int NoModulNow
        {
            get { return this._noModulNow; }
            set
            {
                if (_noModulNow != value)
                {
                    this._noModulNow = value;
                    OnPropertyChanged("NoModulNow");

                    this.UpdateUiIOCheck();
                }
            }
        }
        private void UpdateUiIOCheck()
        {
            try
            {
                if (this.StopReadPLC())
                {
                    this.updateUI();
                }
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Update UI IO Check Error: " + ex.Message));
            }
        }
        private int _noModulTotal;
        public int NoModulTotal
        {
            get { return this._noModulTotal; }
            set
            {
                if (this._noModulTotal != value)
                {
                    this._noModulTotal = value;
                    this.OnPropertyChanged("NoModulTotal");
                }
            }
        }

        private Thread ReadDataPLCInput;
        private Thread ReadDataPLCOutput;
        private MyLogger logger = new MyLogger("PG_IO");

        public PgIO()
        {
            InitializeComponent();


            this.DataContext = this;

            // Common
            this.Loaded += this.PgIOCheck_Loaded;
            this.Unloaded += this.PgIOCheck_UnLoaded;

            this.btIOInput.Click += this.BtIOInput_Click;
            this.btIOOutput.Click += this.BtIOOutput_Click;

            this.btIncNoModulNow.Click += this.BtIncNoModulNow_Click;
            this.btDecNoModulNow.Click += this.BtDecNoModulNow_Click;


        }
        private Boolean StopReadPLC()
        {
            var ret = true;
            if (this.ReadDataPLCInput != null)
            {
                this.exitFlagInput.Set();
                Thread.Sleep(200);
            }
            if (this.ReadDataPLCOutput != null)
            {
                this.exitFlagOutput.Set();
                Thread.Sleep(200);
            }
            return ret;
        }
        private void BtIncNoModulNow_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var btName = button.Name;
            try
            {


                if (this.NoModulNow < this.NoModulTotal - 1)
                {
                    this.NoModulNow = this.NoModulNow + 1;
                }
                else
                {
                    this.NoModulNow = 0;
                }
                UpdateModul();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Action Button {0} Error: ", btName) + ex.Message);
            }

        }
        private void BtDecNoModulNow_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var btName = button.Name;
            try
            {

                if (this.NoModulNow > 0)
                {
                    this.NoModulNow = this.NoModulNow - 1;
                }
                else
                {
                    this.NoModulNow = this.NoModulTotal - 1;
                }
                UpdateModul();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Action Button {0} Error: ", btName) + ex.Message);
            }
        }
        private void UpdateModul()
        {
            this.ModulNow.Content = NoModulNow.ToString();
            this.ModulTotal.Content = NoModulTotal - 1;
        }
        private void BtIOInput_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var btName = button.Name;
            try
            {


                this.NoModulNow = 0;

                if (!this.isInputActive)
                {
                    this.isInputActive = true;
                }

                this.UpdateUiIOCheck();
                this.UpdateModul();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Action Button {0} Error: ", btName) + ex.Message);
            }
        }
        private void BtIOOutput_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var btName = button.Name;
            try
            {


                this.NoModulNow = 0;
                if (this.isInputActive)
                {
                    this.isInputActive = false;
                }

                this.UpdateUiIOCheck();
                this.UpdateModul();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Action Button {0} Error: ", btName) + ex.Message);
            }
        }
        private void PgIOCheck_Loaded(object sender, RoutedEventArgs e)
        {
            Page page = sender as Page;
            var pgName = page.Name;
            try
            {



                // Show  Input
                this.isInputActive = true;

                // Default No. Modul Now When Loaded Page
                this.NoModulNow = 0;

                this.updateUI();
                this.UpdateModul();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Page {0} Loaded Error: {1}.", pgName, ex.Message));
            }
        }
        private List<IoPort> CreateListPointInput(int NoModulNowInput)
        {
            var ret = new List<IoPort>();
            try
            {
                var dataOfPoint = UiManager.fileInput.GetKeyName(UiManager.SectionNameInput[NoModulNowInput]);
                for (int i = 0; i < dataOfPoint.Length; i++)
                {
                    var startusOfPoint = UiManager.fileInput.GetValue(dataOfPoint[i], UiManager.SectionNameInput[NoModulNowInput]);
                    var InputPoint = new IoPort(dataOfPoint[i], startusOfPoint, i.ToString());
                    ret.Add(InputPoint);
                }
            }
            catch (Exception ex)
            {
                ret = null;
                Logger.Create(string.Format("Create List Point Input Error:" + ex.Message));
            }
            return ret;
        }
        private List<IoPort> CreateListPointOutput(int NoModulNowOutput)
        {
            var ret = new List<IoPort>();
            try
            {
                if (UiManager.SectionNameOutput.Length > NoModulNowOutput)
                {
                    var dataOfPoint = UiManager.fileOutput.GetKeyName(UiManager.SectionNameOutput[NoModulNowOutput]);
                    for (int i = 0; i < dataOfPoint.Length; i++)
                    {
                        var startusOfPoint = UiManager.fileOutput.GetValue(dataOfPoint[i], UiManager.SectionNameOutput[NoModulNowOutput]);
                        var InputPoint = new IoPort(dataOfPoint[i], startusOfPoint, i.ToString());
                        ret.Add(InputPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                ret = null;
                Logger.Create(string.Format("Create List Point Output Error:" + ex.Message));
            }
            return ret;
        }
        private void PgIOCheck_UnLoaded(object sender, RoutedEventArgs e)
        {
            Page page = sender as Page;
            var pgName = page.Name;

            try
            {


                // Stop Thread Read Data PLC
                this.StopReadPLC();
            }
            catch (Exception ex)
            {
                Logger.Create(String.Format("Page {0} Unloaded Error: {1}.", pgName, ex.Message));
            }
        }
        private void updatEStatus_Input()
        {
            //if (UiManager.SectionNameInput.Length > this.NoModulNow * 2)
            //{
            //    var bitsInput1 = UiManager.PLC.ReadMultiBits(FENETProtocolDeviceName.BIT_PX, UiManager.SectionNameInput[this.NoModulNow * 2], (UInt16)this.listPointInput1.Count);
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        if (this.listPointInput1 != null & this.listPointInput1.Count > 0)
            //        {
            //            for (int i = 0; i < this.listPointInput1.Count; i++)
            //            {
            //                var port1 = this.listPointInput1[i];
            //                port1.Status = bitsInput1[i] ? "ON" : "OFF";
            //                port1.UpdateUI();
            //            }
            //        }
            //    });
            //}
            //if (this.exitFlagInput.WaitOne(1))
            //{
            //    return;
            //}
            //if (UiManager.SectionNameInput.Length > this.NoModulNow * 2 + 1)
            //{
            //    var bitsInput2 = UiManager.PLC.ReadMultiBits(FENETProtocolDeviceName.BIT_PX, UiManager.SectionNameInput[this.NoModulNow * 2 + 1], (UInt16)this.listPointInput2.Count);
            //    if (this.listPointInput2 != null & this.listPointInput2.Count > 0)
            //    {
            //        for (int i = 0; i < this.listPointInput2.Count; i++)
            //        {
            //            var port2 = this.listPointInput2[i];
            //            port2.Status = bitsInput2[i] ? "ON" : "OFF";
            //            port2.UpdateUI();
            //        }
            //    }
            //}
        }
        private void updatEStatus_Output()
        {
            //if (UiManager.SectionNameOutput.Length > this.NoModulNow * 2)
            //{
            //    var bitsOutput1 = UiManager.PLC.ReadMultiBits(FENETProtocolDeviceName.BIT_PX, UiManager.SectionNameOutput[this.NoModulNow * 2], (UInt16)this.listPointOutput1.Count);
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        if (this.listPointOutput1 != null & this.listPointOutput1.Count > 0)
            //        {
            //            for (int i = 0; i < this.listPointOutput1.Count; i++)
            //            {
            //                var port1 = this.listPointOutput1[i];
            //                port1.Status = bitsOutput1[i] ? "ON" : "OFF";
            //                port1.UpdateUI();
            //            }
            //        }

            //    });
            //}
            //if (this.exitFlagOutput.WaitOne(1))
            //{
            //    return;
            //}
            //if (UiManager.SectionNameOutput.Length > this.NoModulNow * 2 + 1)
            //{
            //    var bitsOutput2 = UiManager.PLC.ReadMultiBits(FENETProtocolDeviceName.BIT_PX, UiManager.SectionNameOutput[this.NoModulNow * 2 + 1], (UInt16)this.listPointOutput2.Count);
            //    if (this.listPointOutput2 != null & this.listPointOutput2.Count > 0)
            //    {
            //        for (int i = 0; i < this.listPointOutput2.Count; i++)
            //        {
            //            var port2 = this.listPointOutput2[i];
            //            port2.Status = bitsOutput2[i] ? "ON" : "OFF";
            //            port2.UpdateUI();
            //        }
            //    }
            //}
        }
        private void generateCells(List<IoPort> portlistGrid1, List<IoPort> portlistGrid2)
        {
            // Create Table Cell 1
            this.gridColum1.Children.Clear();
            this.gridColum1.RowDefinitions.Clear();
            if (portlistGrid1 != null & portlistGrid1.Count > 0)
            {
                for (int i = 0; i < ROW_CNT; i++)
                {
                    var rowDef = new RowDefinition();
                    rowDef.Height = new GridLength(1, GridUnitType.Star);
                    this.gridColum1.RowDefinitions.Add(rowDef);
                }
                this.AddHandler(this.gridColum1);
                for (int i = 0; i < portlistGrid1.Count; i++)
                {
                    if (i < 16)
                    {
                        this.addPort(this.gridColum1, i + 1, portlistGrid1[i]);
                    }
                }
            }
            // Create Table Cell 2
            this.gridColum2.Children.Clear();
            this.gridColum2.RowDefinitions.Clear();
            if (portlistGrid2 != null & portlistGrid2.Count > 0)
            {
                for (int i = 0; i < ROW_CNT; i++)
                {
                    var rowDef = new RowDefinition();
                    rowDef.Height = new GridLength(1, GridUnitType.Star);
                    this.gridColum2.RowDefinitions.Add(rowDef);
                }
                this.AddHandler(this.gridColum2);
                for (int i = 0; i < portlistGrid2.Count; i++)
                {
                    if (i < 16)
                    {
                        this.addPort(this.gridColum2, i + 1, portlistGrid2[i]);
                    }
                }
            }
        }
        private void AddHandler(Grid grid)
        {
            var Cell = new Label();
            Cell.Content = "Port";
            Cell.Background = Brushes.CadetBlue;
            Cell.Foreground = Brushes.White;
            Cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            Cell.VerticalContentAlignment = VerticalAlignment.Center;
            Cell.FontSize = 20;
            Cell.FontWeight = FontWeights.Bold;
            Cell.Margin = new Thickness(2, 0, 0, 0);
            Cell.BorderThickness = new Thickness(2);
            Cell.BorderBrush = Brushes.Red;
            grid.Children.Add(Cell);
            Grid.SetRow(Cell, 0);
            Grid.SetColumn(Cell, 0);

            Cell = new Label();
            Cell.Content = "Label";
            Cell.Background = Brushes.CadetBlue;
            Cell.Foreground = Brushes.White;
            Cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            Cell.VerticalContentAlignment = VerticalAlignment.Center;
            Cell.FontSize = 20;
            Cell.FontWeight = FontWeights.Bold;
            Cell.Margin = new Thickness(1, 0, 0, 0);
            Cell.BorderThickness = new Thickness(2);
            Cell.BorderBrush = Brushes.Red;
            grid.Children.Add(Cell);
            Grid.SetRow(Cell, 0);
            Grid.SetColumn(Cell, 2);

            Cell = new Label();
            Cell.Content = "I/O Name";
            Cell.Background = Brushes.CadetBlue;
            Cell.Foreground = Brushes.White;
            Cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            Cell.VerticalContentAlignment = VerticalAlignment.Center;
            Cell.FontSize = 20;
            Cell.FontWeight = FontWeights.Bold;
            Cell.Margin = new Thickness(1, 0, 0, 0);
            Cell.BorderThickness = new Thickness(2);
            Cell.BorderBrush = Brushes.Red;
            grid.Children.Add(Cell);
            Grid.SetRow(Cell, 0);
            Grid.SetColumn(Cell, 4);

            Cell = new Label();
            Cell.Content = "Status";
            Cell.Background = Brushes.CadetBlue;
            Cell.Foreground = Brushes.White;
            Cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            Cell.VerticalContentAlignment = VerticalAlignment.Center;
            Cell.FontSize = 20;
            Cell.FontWeight = FontWeights.Bold;
            Cell.Margin = new Thickness(1, 0, 0, 0);
            Cell.BorderThickness = new Thickness(2);
            Cell.BorderBrush = Brushes.Red;
            grid.Children.Add(Cell);
            Grid.SetRow(Cell, 0);
            Grid.SetColumn(Cell, 6);
        }
        private void addPort(Grid grid, int rowIndex, IoPort port)
        {
            var cell = new Label();
            cell.Content = port.NoDisplay;
            cell.Background = Brushes.CadetBlue;
            cell.Foreground = Brushes.White;
            cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            cell.FontSize = 15;
            cell.FontWeight = FontWeights.Bold;
            cell.Margin = new Thickness(2, 0, 0, 0);
            cell.BorderThickness = new Thickness(2);
            cell.BorderBrush = Brushes.LightGray;
            grid.Children.Add(cell);
            Grid.SetRow(cell, rowIndex);
            Grid.SetColumn(cell, 0);

            cell = new Label();
            cell.Content = port.PortAddr;
            cell.Background = Brushes.CadetBlue;
            cell.Foreground = Brushes.White;
            cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            cell.FontSize = 15;
            cell.FontWeight = FontWeights.Bold;
            cell.Margin = new Thickness(2, 0, 0, 0);
            cell.BorderThickness = new Thickness(2);
            cell.BorderBrush = Brushes.LightGray;
            grid.Children.Add(cell);
            Grid.SetRow(cell, rowIndex);
            Grid.SetColumn(cell, 2);

            cell = new Label();
            cell.Content = port.Name;
            cell.Background = Brushes.White;
            cell.Foreground = Brushes.Black;
            cell.HorizontalContentAlignment = HorizontalAlignment.Left;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            cell.FontSize = 15;
            cell.FontWeight = FontWeights.Bold;
            cell.Margin = new Thickness(2, 0, 0, 0);
            cell.BorderThickness = new Thickness(2);
            cell.BorderBrush = Brushes.LightGray;
            grid.Children.Add(cell);
            Grid.SetRow(cell, rowIndex);
            Grid.SetColumn(cell, 4);

            cell = new Label();
            cell.Foreground = Brushes.White;
            cell.HorizontalContentAlignment = HorizontalAlignment.Center;
            cell.VerticalContentAlignment = VerticalAlignment.Center;
            cell.FontSize = 15;
            cell.FontWeight = FontWeights.Bold;
            cell.Margin = new Thickness(2, 0, 0, 0);
            cell.BorderThickness = new Thickness(2);
            cell.BorderBrush = Brushes.LightGray;
            grid.Children.Add(cell);
            Grid.SetRow(cell, rowIndex);
            Grid.SetColumn(cell, 6);
            this.bindCell(port, cell);
        }
        private void bindCell(IoPort port, Label cell)
        {
            var b1 = new Binding("Status");
            b1.Source = port;
            b1.Mode = BindingMode.OneWay;
            cell.SetBinding(Label.ContentProperty, b1);

            var b2 = new Binding("StatusColor");
            b2.Source = port;
            b2.Mode = BindingMode.OneWay;
            cell.SetBinding(Label.BackgroundProperty, b2);
        }
        private void updateUI()
        {
            if (this.isInputActive)
            {
                this.rectInput.Fill = Brushes.LimeGreen;
                this.rectOutput.Fill = Brushes.DarkGray;
                this.NoModulTotal = UiManager.SectionNameInput.Length / 2 + UiManager.SectionNameInput.Length % 2;

                this.listPointInput1 = new List<IoPort>();
                if (UiManager.SectionNameInput.Length > this.NoModulNow * 2)
                {
                    this.listPointInput1 = this.CreateListPointInput(this.NoModulNow * 2);
                }

                this.listPointInput2 = new List<IoPort>();
                if (UiManager.SectionNameInput.Length > this.NoModulNow * 2 + 1)
                {
                    this.listPointInput2 = this.CreateListPointInput(this.NoModulNow * 2 + 1);
                }

                // Update Auto Generate Cell
                this.generateCells(this.listPointInput1, this.listPointInput2);

                // start Thread Read Data
                this.ReadDataPLCInput = new Thread(new ThreadStart(() =>
                {
                    // Reset Exit Flag
                    this.exitFlagInput.Reset();

                    while (true)
                    {
                        try
                        {
                            // Check Exit
                            if (this.exitFlagInput.WaitOne(1))
                            {
                                break;
                            }
                            this.updatEStatus_Input();
                        }
                        catch (Exception ex)
                        {
                            Logger.Create(String.Format("Update Status Error: " + ex.Message));
                        }
                    }
                }));
                this.ReadDataPLCInput.IsBackground = true;
                this.ReadDataPLCInput.Start();
            }
            else
            {
                this.rectInput.Fill = Brushes.DarkGray;
                this.rectOutput.Fill = Brushes.LimeGreen;
                this.NoModulTotal = UiManager.SectionNameOutput.Length / 2 + UiManager.SectionNameOutput.Length % 2;

                this.listPointOutput1 = new List<IoPort>();
                if (UiManager.SectionNameOutput.Length > this.NoModulNow * 2)
                {
                    this.listPointOutput1 = this.CreateListPointOutput(this.NoModulNow * 2);
                }

                this.listPointOutput2 = new List<IoPort>();
                if (UiManager.SectionNameOutput.Length > this.NoModulNow * 2 + 1)
                {
                    this.listPointOutput2 = this.CreateListPointOutput(this.NoModulNow * 2 + 1);
                }
                // Update Auto Generate Cell
                this.generateCells(this.listPointOutput1, this.listPointOutput2);

                // start Thread Read Data
                this.ReadDataPLCOutput = new Thread(new ThreadStart(() =>
                {
                    //Reset Exit Flag
                    this.exitFlagOutput.Reset();

                    while (true)
                    {
                        try
                        {
                            // Check Exit
                            if (this.exitFlagOutput.WaitOne(1))
                            {
                                break;
                            }
                            this.updatEStatus_Output();
                        }
                        catch (Exception ex)
                        {
                            Logger.Create(String.Format("Update Status Error: " + ex.Message));
                        }
                    }
                }));
                this.ReadDataPLCOutput.IsBackground = true;
                this.ReadDataPLCOutput.Start();
            }
        }



    }
    class IoPort : INotifyPropertyChanged
    {
        private Brush ON_COLOR = Brushes.Green;
        private Brush OFF_COLOR = Brushes.Brown;

        public string NoDisplay { get; set; }
        public string PortAddr { get; set; }
        public string Name { get; set; }
        private string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                if (_status != null & _status.Equals("ON"))
                {
                    this.StatusColor = ON_COLOR;
                }
                else
                {
                    this.StatusColor = OFF_COLOR;
                }
            }
        }
        public Brush StatusColor { get; private set; }
        public IoPort(string portAddr, String name, String noDisplay)
        {
            this.NoDisplay = noDisplay;
            this.PortAddr = portAddr;
            this.Name = name;
            this.Status = "OFF";
            this.StatusColor = OFF_COLOR;
        }
        public void UpdateUI()
        {
            OnPropertyChanged("PortId");
            OnPropertyChanged("Name");
            OnPropertyChanged("Status");
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("NoDisplay");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

}
