using System;
using System.IO;


class MainClass
{
    private static readonly int MAX_FW_SIZE   = 0x60000; //Max is 384KB (128KB are reserved for Bootloader)
    private static readonly int FW_BLOCK_SIZE = 2048;

    static int Main(string[] args)
    {
        // Test if input arguments were supplied:
        if (args.Length != 1)
        {
            System.Console.WriteLine("FWencoder v0.1");
            System.Console.WriteLine("Please enter binary FW file name.");
            System.Console.WriteLine("Usage: FWencoder <input FW binary file>");
            return -1;
        }

        // Read input file
        StreamReader streamReader;
        try
        {
           streamReader = new StreamReader(args[0]);
        }
        catch (Exception)
        {
           System.Console.WriteLine("Unable to open FW file:" + args[0]);
           return -4;
        }
        FileInfo fileInfo = new FileInfo(args[0]);
        if (fileInfo.Length > MAX_FW_SIZE)
        {
           System.Console.WriteLine("FW file is too big:" + args[0]);
           streamReader.Close();
           return -3;
        }
        byte[] fileDat = new byte[fileInfo.Length];
        streamReader.BaseStream.Read(fileDat, 0, fileDat.Length);
        streamReader.Close();
        //Calculate blocks to encode
        int numBlocks = (fileDat.Length / FW_BLOCK_SIZE);
        System.Console.WriteLine("FW blocks: {0}", numBlocks);
        if ((numBlocks*FW_BLOCK_SIZE) != fileDat.Length)
        {
           System.Console.WriteLine("Input file size seems to be wrong, it is: {0} and should be: {1} bytes", fileDat.Length, (numBlocks*FW_BLOCK_SIZE));
           System.Console.WriteLine("Aborting!");
           return -2;
        }
        else
        {
           // Generate the FW file.
          if (numBlocks > 0)
          {
              //Save FW
              string filePath = AppDomain.CurrentDomain.BaseDirectory + "\\update.lcd";
              Codec.CoDec FWCodec = new Codec.CoDec();
              if (FWCodec.PutFW(filePath, fileDat, numBlocks) == 0)
              {
                  System.Console.WriteLine("update.lcd file generated!");
                  return 0;
              }
              else
              {
                  System.Console.WriteLine("Unable to generate FW file");
                  return -5;
              }
          }
          else
          {
              System.Console.WriteLine("No data to generate a FW file!");
              return -6;
          }
        }
    }
}

