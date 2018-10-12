using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
        
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                Scroll.PageDown();
                Scroll.LineUp();
                Scroll.LineUp();
                e.Handled = true;
            }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetFont();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetFont();
        }

        private void ResetFont()
        {
            string rulerText = new string('啊', 40);
            for (double fontSize = 9; fontSize < 60; fontSize += 1)
            {
                var formattedText = new FormattedText(rulerText,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(ContentText.FontFamily, ContentText.FontStyle, ContentText.FontWeight, ContentText.FontStretch),
                    fontSize,
                    Brushes.Black,
                    new NumberSubstitution());
                if (formattedText.Width >= (this.ActualWidth * 35 / 40))
                {
                    break;
                }
                ContentText.Width = formattedText.Width;
                ContentText.FontSize = fontSize;
            }
        }
    }
}
