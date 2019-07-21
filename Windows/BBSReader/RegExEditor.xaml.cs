using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BBSReader
{
    /// <summary>
    /// RegExEditor.xaml 的交互逻辑
    /// </summary>
    public partial class RegExEditor : Window
    {
        public string text;
        private ObservableCollection<string> contents;

        public RegExEditor()
        {
            InitializeComponent();

            contents = new ObservableCollection<string>();
            ContentListView.DataContext = contents;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Run();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Run();
        }

        private void Run()
        {
            Func<int, int, string> generateChapter = delegate (int from, int to)
            {
                string title = text.Substring(from);
                title = title.Substring(0, title.IndexOf("\n") - 1);
                return title;
            };
            contents.Clear();
            string pattern = RegExp.Text;
            int last = 0;
            foreach (Match m in Regex.Matches(text, pattern))
            {
                int i = m.Index;
                if (!text.Substring(last, i - last).Contains("\n"))
                {
                    continue;
                }
                contents.Add(generateChapter(last, i));
                last = i;
            }
            if (contents.Count != 0)
            {
                if (last < text.Length)
                {
                    contents.Add(generateChapter(last, text.Length));
                }
            }
        }
    }
}
