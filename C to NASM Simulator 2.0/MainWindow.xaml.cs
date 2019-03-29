using C_to_NASM_Simulator_2._0.Core;
using C_to_NASM_Simulator_2._0.Types;
using C_to_NASM_Simulator_2._0.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace C_to_NASM_Simulator_2._0
{
    /// <summary>
    /// UI logic
    /// </summary>
    public partial class MainWindow : Window
    {
        public UserInterface ui = Emulator.UI;

        public MainWindow()
        {
            InitializeComponent();
            OutputList.ItemsSource = ui.ObservableLines;
            StackList.ItemsSource = ui.Stack;
            DataContext = ui;
            MemoryStructure.DataContext = new[] { ui.MemoryMap };

        }

        private void InputText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string input = new TextRange(InputText.Document.ContentStart, InputText.Document.ContentEnd).Text;
            /*if (!String.IsNullOrWhiteSpace(input) && !String.IsNullOrEmpty(input))
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
            }*/
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
            Utils.RefreshSimulator();
            ui.ObservableLines.Clear();
            int lineIndex = 0;
            int startIndex = 0;
            string input = new TextRange(InputText.Document.ContentStart, InputText.Document.ContentEnd).Text;
            var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var compiledLines = new List<String>();
            try
            {
                lines.ForEach(line => compiledLines.AddRange(new Compiler(line).Output
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)));
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Incorrect input!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Utils.IfElseFix(Utils.StartLabelFix(compiledLines))
                .ForEach(line => {                               
                    ui.ObservableLines.Add($"{lineIndex} {line}");
                    if (line.Equals("START:")) startIndex = lineIndex;
                    lineIndex++;                   
                });
            OutputList.SelectedIndex = ui.ObservableLines.IndexOf($"{startIndex} START:");
            ui.ObservableLines.Add($"{lineIndex} HLT");
            /*string temp = string.Empty; for test purpose to view all lines in string
            ui.ObservableLines.ToList().ForEach(str => temp += $"{str}{Environment.NewLine}");*/
            Utils.LoadVarsToEmu();
        }

        private void NextCmdButton_Click(object sender, RoutedEventArgs e)
        {
            var emu = new Emulator(this);
            emu.NextStep();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var fileName = $"{Path.GetTempFileName()}.docx".Replace(".tmp", "");
            File.WriteAllBytes(fileName, Properties.Resources.Doc);
            Process.Start(fileName);
        }
    }
}
