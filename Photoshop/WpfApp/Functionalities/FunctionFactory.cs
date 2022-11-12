using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Functionalities.Implementations;

namespace WpfApp.Functionalities
{
    public class FunctionFactory
    {
        private Logger logger;
        private Dictionary<ProgramFunction, FunctionInterface> functions;

        public FunctionFactory()
        {
            this.logger = new Logger();
            this.functions = new Dictionary<ProgramFunction, FunctionInterface>();
            this.functions.Add(ProgramFunction.SudokuChecker, new SudokuChecker(this.logger));
            this.functions.Add(ProgramFunction.Ps_Negalas, new Ps_Negalas(this.logger));
            this.functions.Add(ProgramFunction.Ps_Gamma_Transzformacio, new Ps_GammaTranszformacio(this.logger));
            this.functions.Add(ProgramFunction.Ps_Logaritmikus_Transzformacio, new Ps_LogaritmikusTranszformacio(this.logger));
            this.functions.Add(ProgramFunction.Ps_Szurkites, new Ps_Szurkites(this.logger));
        }

        public Logger GetLogger()
        {
            return this.logger;
        }

        public FunctionInterface GetFunction(ProgramFunction function)
        {
            return this.functions[function];
        }
    }
}
