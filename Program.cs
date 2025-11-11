using System;
using System.Linq;
using System.Text;
namespace LR1_1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Лабораторная работа №1. Разработка консольного приложения.");
            Console.WriteLine("Морозова Анна Алексеевна");
            Console.WriteLine("ИДБ-23-01 Направление: 09.03.01");
            Console.WriteLine("09.11.2005");
            Console.WriteLine("г. Москва");
            Console.WriteLine("Информатика");
            Console.WriteLine("Люблю рукоделие, рисование и, конечно же, программирование!");
            Console.WriteLine("");
            Console.WriteLine("Задание 2. Вариант 14.");
            float t;
            int a = 10, z = 5, Et = 6;
            t = 35/a * z + z * a - (a + Et)/4;
            Console.WriteLine($"Значения переменных: a = {a}, z = {z}, Et = {Et}");
            Console.WriteLine($"Значение выражения: t = {t}");
        }
    }
}