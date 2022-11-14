using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuChecker.Functionalities
{
    public class Logger
    {
        private ObservableCollection<string> logs;
        public ObservableCollection<string> Logs { get => this.logs; }

        public Logger()
        {
            this.logs = new ObservableCollection<string>();
        }

        public void Log(string message)
        {
            this.logs.Add(message);
        }
    }
}
