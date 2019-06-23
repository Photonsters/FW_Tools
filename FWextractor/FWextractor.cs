using System;
using System.IO;
//using System.Runtime.InteropServices;

class MainClass
{
		private static readonly int MAX_FW_SIZE = 0x60000; //Max is 384KB (128KB are reserved for Bootloader)
		
    static int Main(string[] args)
    {
        // Test if input arguments were supplied:
        if (args.Length != 2)
        {
            System.Console.WriteLine("FWextractor v0.1");
            System.Console.WriteLine("Please enter binary FW package and output file name.");
            System.Console.WriteLine("Usage: FWextractor <input FW binary> <output decoded FW binary>");
            return 1;
        }

        // Decode the FW.
        byte[] FWdata = new byte[MAX_FW_SIZE];
        Codec.CoDec extractor = new Codec.CoDec();
        int result = extractor.GetFW(args[0], FWdata);
        if (result > 0)
        {
					StreamWriter streamWriter;
					try
					{
						streamWriter = new StreamWriter(args[1]);
					}
					catch (Exception)
					{
						System.Console.WriteLine("Unable to generate output file:" + args[1]);
						return -1;
					}
					streamWriter.BaseStream.Write(FWdata, 0, result*2048);
					streamWriter.Close();
          System.Console.WriteLine("FW extracted ({0} blocks)", result);
          return 0;
            
        }
        else
        {
            System.Console.WriteLine("FW extraction failed!");
            return 1;
        }
    }
}

