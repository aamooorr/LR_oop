using System;
using System.IO;

namespace LR1_3
{
    class Program
    {
        static void Main(string[] args)
        {
            TextWriter save_out = Console.Out;
            TextReader save_in = Console.In;

            using (var new_out = new StreamWriter(@"output.txt"))
            using (var new_in = new StreamReader(@"input.txt"))
            {
                Console.SetOut(new_out);
                Console.SetIn(new_in);

                int t = Convert.ToInt32(Console.ReadLine());
                int N = Convert.ToInt32(Console.ReadLine());
                double X = Convert.ToDouble(Console.ReadLine());
                double Y = Convert.ToDouble(Console.ReadLine());

                double Z = 0;
                int i = 1;
                double denominator1 = 1, denominator2 = 2;

                if (t == 0) 
                {
                    for (i = 1; i <= N; i++)
                    {
                        double term;
                        if (i % 2 == 1) 
                        {
                            term = (Math.Pow(Y, i) + Math.Pow(X, i)) / (denominator1 * denominator2);
                        }
                        else 
                        {
                            term = (Math.Pow(X, i) - Math.Pow(Y, i)) / (denominator1 * denominator2);
                        }
                        Z += term;

                        
                        denominator1 = denominator2;
                        denominator2 += 2;
                    }
                }

                else if (t == 1) 
                {
                    i = 1;
                    denominator1 = 1;
                    denominator2 = 2;
                    while (i <= N)
                    {
                        double term;
                        if (i % 2 == 1)
                        {
                            term = (Math.Pow(Y, i) + Math.Pow(X, i)) / (denominator1 * denominator2);
                        }
                        else
                        {
                            term = (Math.Pow(X, i) - Math.Pow(Y, i)) / (denominator1 * denominator2);
                        }
                        Z += term;

                        denominator1 = denominator2;
                        denominator2 += 2;
                        i++;
                    }
                }

                else if (t == 2) 
                {
                    i = 1;
                    denominator1 = 1;
                    denominator2 = 2;
                    do
                    {
                        double term;
                        if (i % 2 == 1)
                        {
                            term = (Math.Pow(Y, i) + Math.Pow(X, i)) / (denominator1 * denominator2);
                        }
                        else
                        {
                            term = (Math.Pow(X, i) - Math.Pow(Y, i)) / (denominator1 * denominator2);
                        }
                        Z += term;

                        denominator1 = denominator2;
                        denominator2 += 2;
                        i++;
                    } while (i <= N);
                }

                Console.WriteLine(string.Format("{0:0.0000000}", Z));
            }

            Console.SetOut(save_out);
            Console.SetIn(save_in);
        }
    }
}