using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace daesung_4chamber_watcher
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Process[] daesung_4chamber_app = Process.GetProcessesByName("DaesungEntCleanOven4");
                    System.Console.WriteLine("running daesung.4chamber.app count = {0}", daesung_4chamber_app.Length);
                    if (daesung_4chamber_app.Length < 1)
                    {
                        string proc_file = @"D:\dsent_clean_oven_4ch\DaesungEntCleanOven4.exe";
                        if (!System.IO.File.Exists(proc_file))
                        {
                            System.Console.WriteLine("daesung_4chamber.exe not found. path = {0}", proc_file);
                        }
                        else
                        {
                            System.Console.WriteLine(@"Launch... {0}", proc_file);
                            Process.Start(proc_file);
                            System.Threading.Thread.Sleep(10 * 1000);
                        }
                    }
                    System.Threading.Thread.Sleep(5 * 1000);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("daesung_4chamber_watcher. exception : {0}", ex.Message);
                }
            }
        }
    }
}
