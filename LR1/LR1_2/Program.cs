using System;
using System.IO;

namespace LR1_2
{

    class Program
    {
        public static void Main(string[] args)
        {
            TextWriter save_out = Console.Out;
            TextReader save_in = Console.In;
            var new_out = new StreamWriter(@"output.txt");
            var new_in = new StreamReader(@"input.txt");
            Console.SetOut(new_out);
            Console.SetIn(new_in);

            double apo, boo, css, ddd, ew;
            double f, g;
            apo = Convert.ToDouble(Console.ReadLine());
            boo = Convert.ToDouble(Console.ReadLine());
            css = Convert.ToDouble(Console.ReadLine());
            ddd = Convert.ToDouble(Console.ReadLine());
            ew = Convert.ToDouble(Console.ReadLine());

            if (boo == 0 || css == 0 || ddd == 0)
            {
                Console.WriteLine("ERROR");
            }
            else
            {
                f = Math.Round(apo/boo + boo/css + css/ddd);
                Console.WriteLine(f);
            }

            if (ddd - Math.Sqrt(Math.Abs(ew - apo)) < 0)
            {
                Console.WriteLine("ERROR");
            }
            else
            {
                g = Math.Round(Math.Sqrt(ddd - Math.Sqrt(Math.Abs(ew - apo))));
                Console.WriteLine(g);
            }

            Console.SetOut(save_out); new_out.Close();
            Console.SetIn(save_in); new_in.Close();
        }
    }
}