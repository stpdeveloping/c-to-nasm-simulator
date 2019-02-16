using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace C_to_NASM_Simulator_2._0
{
    /// <summary>
    /// UI logic
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<String> ObservableLines = new ObservableCollection<String>();

        public MainWindow()
        {
            InitializeComponent();
            OutputList.ItemsSource = ObservableLines;
        }

        private void InputText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = new TextRange(InputText.Document.ContentStart, InputText.Document.ContentEnd).Text;
            if (!String.IsNullOrWhiteSpace(input) && !String.IsNullOrEmpty(input))
            {
                var _input = input.Trim();
                if (_input.ElementAt(_input.Length - 1).Equals(';'))
                {
                    InputText.TextChanged -= new TextChangedEventHandler(InputText_TextChanged);
                    InputText.Document.Blocks.Clear();
                    var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    InputText.AppendText(lines.First());
                    lines.RemoveAt(0);
                    lines.ForEach(str => InputText.AppendText(Environment.NewLine + str));
                    InputText.AppendText(Environment.NewLine);
                    InputText.CaretPosition = InputText.CaretPosition.DocumentEnd;
                    InputText.TextChanged += new TextChangedEventHandler(InputText_TextChanged);
                }
            }
            var enumerator = input.GetEnumerator();
            int duplicateSpace = 0;
            while (enumerator.MoveNext())
            {             
                if (enumerator.Current.Equals(' '))
                {
                    duplicateSpace++;
                    CompileButton.IsEnabled = duplicateSpace > 1 ? false : true;
                }
                else
                    duplicateSpace = 0;

            }
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            Utils.RefreshCompiler();
            ObservableLines.Clear();
            string input = new TextRange(InputText.Document.ContentStart, InputText.Document.ContentEnd).Text;
            var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var compiledLines = new List<String>();
            lines.ForEach(line => compiledLines.AddRange(new Compiler(line).Output
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)));
            Utils.ifElseFix(compiledLines).ForEach(line => ObservableLines.Add(line));
        }
    }
}
