using C_to_NASM_Simulator_2._0.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace C_to_NASM_Simulator_2._0.Core
{
    public class UserInterface : INotifyPropertyChanged
    {
        private int ax;
        private int bx;
        private int ip;

        public Memory MemoryMap = new Memory();

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

        public int IP
        {
            get { return ip; }
            set
            {
                ip = value;
                OnPropertyChanged(nameof(ip));
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
