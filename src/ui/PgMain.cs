using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PgMain : Page
    {
        private String lastLog = "";
        private String lastLog2 = "";
        private int gLogIndex = 0;
        public Boolean uiLogEnable { get; set; } = true;
        public ObservableCollection<logEntry> LogEntries { get; set; } = new ObservableCollection<logEntry>();
        private bool autoScrollMode = true;
        bool isAlarming = false;

        private bool MesAcept = false;
        private void McServerComm_ConnectionChanged(EndPoint remoteEP, bool isConnected)
        {
            this.Dispatcher.Invoke(() => {
                MesAcept = isConnected;
                //this.chkMcsAccepted.IsChecked = isConnected;
                //this.chkMcsListen.IsChecked = !isConnected;
                //if (isConnected)
                //{
                //    this.lblMcsStatus.Content = String.Format("Server [{0}] connected", remoteEP.ToString());
                //}
                //else
                //{
                //    this.lblMcsStatus.Content = "Listening...";
                //}
            });
        }
        //private void displayAlarm(Int32 code)
        //{
        //    try
        //    {
        //        //isAlarming  = true;
        //        Qr1Manager.AlarmBegin();
        //        // ALARM.BEGIN event:
        //        // Createlog:
        //        var alarm = new AlarmInfo(code, AlarmInfo.getMessage(code));
        //        DbWrite.createAlarm(alarm);

        //        var evenlog = new EventLog(code);
        //        DbWrite.createEvent(evenlog);

        //        // Set Alarm Bit:


        //        // Display Alarm:
        //        this.Dispatcher.Invoke(() =>
        //        {
        //            var wnd = new WndAlert(code, AlarmInfo.getSolution(code), AlarmInfo.getMessage(code));
        //            wnd.ShowDialog();
        //            var alm = AlarmInfo.getMessage(code);
        //            addLog(alm);
        //            Qr1Manager.AlarmEnd();
        //            //isAlarming = false;
        //        });

        //        // Wait user click OK:
        //        while (isAlarming)
        //        {
        //            Thread.Sleep(100);
        //        }
        //        // ALARM.END event:
        //        Qr1Manager.AlarmEnd();
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Create("displayAlarm error:" + ex.Message);
        //    }
        //}
    }
    public static class ActionClearAlarm
    {
        public static Action ClearErrorAction { get; set; }
    }
    public class logEntry : PropertyChangedBase
    {
        public int logIndex { get; set; }
        public String logTime { get; set; }
        public string logMessage { get; set; }
    }
    public class collapsibleLogEntry : logEntry
    {
        public List<logEntry> Contents { get; set; }
    }
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => {
                    PropertyChangedEventHandler handler = PropertyChanged;
                    if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
                }));
            }
            catch (Exception ex)
            {
                Debug.Write("OnPropertyChanged error:" + ex.Message);
            }
        }
    }
}
