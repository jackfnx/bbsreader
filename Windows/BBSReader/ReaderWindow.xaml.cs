using System.ComponentModel;
using System.Windows;

namespace BBSReader
{
    /// <summary>
    /// ReaderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ReaderWindow : Window
    {
        public ReaderWindow()
        {
            InitializeComponent();
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            if (Stay)
            {
                Hide();
                e.Cancel = true;
            }
            else
            {
                base.OnClosing(e);
            }
        }

        public bool Stay = true;
    }
}
