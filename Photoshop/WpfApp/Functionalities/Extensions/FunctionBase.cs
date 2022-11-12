using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Functionalities.Implementations
{
    public class FunctionBase
    {
        protected ProgramFunction function;
        protected Logger logger;
        protected Stopwatch stopwatch;

        public FunctionBase(ProgramFunction function, Logger logger)
        {
            this.function = function;
            this.logger = logger;
            this.stopwatch = new Stopwatch();
        }

        protected Bitmap LoadImage() { return null; }

        protected void StartTimer()
        {
            this.stopwatch.Start();
        }

        protected void StopTimer()
        {
            this.stopwatch.Stop();
        }

        protected TimeSpan GetElapsedTime()
        {
            return this.stopwatch.Elapsed;
        }

        protected void ResetTimer()
        {
            this.stopwatch.Reset();
        }

        protected void LogFunctionResult()
        {
            this.logger.Log($"Function results: [{this.function}] took [{this.stopwatch.Elapsed}]");
        }
    }
}
