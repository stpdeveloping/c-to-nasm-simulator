using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_to_NASM_Simulator_2._0.Core
{
    public class UserInterface : INotifyPropertyChanged
    {
        private int ax;
        private int bx;
        public int AX
        {
            get { return ax; }
            set
            {
                ax = value;
                OnPropertyChanged(nameof(ax));
            }
        }
        public int BX
        {
            get { return bx; }
            set
            {
                bx = value;
                OnPropertyChanged(nameof(bx));
            }
        }

        public ObservableCollection<string> ObservableLines = new ObservableCollection<string>();
        public ObservableCollection<int> Stack = new ObservableCollection<int>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
