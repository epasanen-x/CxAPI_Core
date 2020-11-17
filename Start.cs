using System;
using System.Collections.Generic;
using System.Diagnostics;



namespace CxAPI_Core
{

    internal class Start
    {
        private static void Main(string[] args)
        {
            dispatcher dsp = new dispatcher();
            try
            {
                Console.WriteLine(Configuration.getVersion());
                resultClass token = dsp.dispatch(args);
                if (token.debug)
                {
                    Console.WriteLine("Successful completion.");
                }
                dsp.Elapsed_Time();
                if (token.test)
                {
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                dsp.Elapsed_Time();
                Console.WriteLine(ex.ToString());
                if (_options.test)
                {
                    Console.ReadKey();
                }
            }
        }

    }

}