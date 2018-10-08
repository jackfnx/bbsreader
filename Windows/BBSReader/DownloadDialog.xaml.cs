using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace BBSReader
{
    /// <summary>
    /// DownloadDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadDialog : Window
    {
        private RunningStatus runningStatus;

        enum RunningStatus {  START,  RUNNING,   COMPLETE };

        public DownloadDialog()
        {
            InitializeComponent();

            this.runningStatus = RunningStatus.START;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.runningStatus == RunningStatus.START)
            {
                Process proc = new Process();
                string script = "-u E:/turboc/bbsreader/Python3/update.py";
                proc.StartInfo.FileName = @"python";
                proc.StartInfo.Arguments = script;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.OutputDataReceived += (s, ev) => this.Dispatcher.BeginInvoke(new Action<string>(OutputLine), ev.Data);
                proc.Exited += (s, ev) =>
                {
                    this.runningStatus = RunningStatus.COMPLETE;
                    this.Dispatcher.BeginInvoke(new Action<string>(OutputLine), "--- OK, press <any key> to continue. ---");
                };
                this.runningStatus = RunningStatus.RUNNING;
                proc.Start();
                proc.BeginOutputReadLine();
            }
            else if (this.runningStatus == RunningStatus.COMPLETE)
            {
                this.DialogResult = true;
            }
        }

        private void OutputLine(string line)
        {
            ConsoleText.Text += line + "\n";
            ConsoleText.ScrollToEnd();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.runningStatus == RunningStatus.COMPLETE)
            {
                this.DialogResult = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.runningStatus == RunningStatus.COMPLETE)
            {
                this.DialogResult = true;
            }
        }
    }
}
