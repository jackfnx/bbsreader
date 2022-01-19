using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace BBSReader
{
    /// <summary>
    /// ScriptDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ScriptDialog : Window
    {
        private RunningStatus runningStatus;

        private enum RunningStatus { RUNNING, COMPLETE };

        private readonly string[] scripts_0 = { "-u E:/turboc/bbsreader/Python3/update.py --bbsid 0", "-u E:/turboc/bbsreader/Python3/update.py --bbsid 1", "-u E:/turboc/bbsreader/Python3/group.py" };
        private readonly string[] scripts_1 = { "-u E:/turboc/bbsreader/Python3/download_detail.py {0} {1} {2} {3}" };
        private readonly ConsoleContent dc = new ConsoleContent();

        public enum ScriptId
        {
            UPDATE_ALL,
            DOWNLOAD_ONE_DETAIL
        }

        private readonly ScriptId scriptId;
        private readonly object[] paras;

        public ScriptDialog(ScriptId scriptId, params object[] paras)
        {
            InitializeComponent();
            DataContext = dc;

            this.scriptId = scriptId;
            this.paras = paras;
            StartScript();
        }

        private void StartScript()
        {
            string[] scripts;
            if (scriptId == ScriptId.UPDATE_ALL)
                scripts = scripts_0;
            else if (scriptId == ScriptId.DOWNLOAD_ONE_DETAIL)
                scripts = scripts_1;
            else
                scripts = scripts_0;
            List<Process> procs = new List<Process>();
            foreach (string script in scripts)
            {
                string script_para = script;
                for (int i = 0; i < paras.Length; i++)
                {
                    script_para = script_para.Replace("{" + i + "}", paras[i].ToString());
                }
                Process proc = new Process();
                proc.StartInfo.FileName = @"python";
                proc.StartInfo.Arguments = script_para;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.OutputDataReceived += (s, ev) => this.Dispatcher.BeginInvoke(new Action<string>(dc.OutputLine), ev.Data);
                proc.ErrorDataReceived += (s, ev) => this.Dispatcher.BeginInvoke(new Action<string>(dc.OutputLine), ev.Data);
                procs.Add(proc);
            }

            for (int i = 0; i < scripts.Length; i++)
            {
                if (i != scripts.Length - 1)
                {
                    Process curr = procs[i];
                    Process next = procs[i + 1];
                    curr.Exited += (s, ev) =>
                    {
                        next.Start();
                        next.BeginOutputReadLine();
                    };
                }
                else
                {
                    procs[i].Exited += (s, ev) =>
                    {
                        this.runningStatus = RunningStatus.COMPLETE;
                        this.Dispatcher.BeginInvoke(new Action<string>(dc.OutputLine), "--- OK, press <any key> to continue. ---");
                    };
                }
            }
            runningStatus = RunningStatus.RUNNING;
            procs[0].Start();
            procs[0].BeginOutputReadLine();
        }
        
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (runningStatus == RunningStatus.COMPLETE)
            {
                DialogResult = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (runningStatus == RunningStatus.COMPLETE)
            {
                DialogResult = true;
            }
        }

        internal class ConsoleContent : INotifyPropertyChanged
        {
            private string consoleInput = string.Empty;
            private ObservableCollection<string> consoleOutput = new ObservableCollection<string> { "BBSReader Console" };

            public string ConsoleInput
            {
                get
                {
                    return consoleInput;
                }
                set
                {
                    consoleInput = value;
                    OnPropertyChanged("consoleInput");
                }
            }

            public ObservableCollection<string> ConsoleOutput
            {
                get
                {
                    return consoleOutput;
                }
                set
                {
                    consoleOutput = value;
                    OnPropertyChanged("consoleOutput");
                }
            }

            public void OutputLine(string line)
            {
                ConsoleOutput.Add(line);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                if (propertyName != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
