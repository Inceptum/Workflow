using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Inceptum.Workflow.Tests
{
    [TestFixture]
    public class InvetsigationTests
    {
        [Test]
        public void Test()
        {
             
            Regex regex=new Regex(@"\*\$\$\$\*
([\d\. :]+) ""\[DiasoftThread\] UpdateBalance""
Limit #1662
Посчитан ([\d\. :]+), закрыто ([\d\. :]+)
Счет 30218810200001000039
Оборот: ([-\d\.,:]+) \(курс [-\d\.,:]+\)
Счет 30219810500001000039
Оборот: ([-\d\.,:]+) \(курс [-\d\.,:]+\)
Счет 30232810600001000039
Оборот: ([-\d\.,:]+) \(курс [-\d\.,:]+\)
Счет 30233810900001000039
Оборот: ([-\d\.,:]+) \(курс [-\d\.,:]+\)
Не-Юнистрим: ([-\d\.,:]+) \(таймаут \d+ мс\)
Текущее сальдо: ([-\d\.,:]+), текущий долг: ([-\d\.,:]+)");
            int i = 0, j = 0;
            var date = @"2013-09-30";

            var log=new StringBuilder();
            using (var fs = File.Open(string.Format(@"d:\tmp\limit\{0}.log.csv",date), FileMode.Create))
            {
                using (var w = new StreamWriter(fs,Encoding.GetEncoding(1251)))
                {
                    w.WriteLine(@"Время;Посчитан;Закрыто;30218810200001000000;30219810500001000000;30232810600001000000;30233810900001000000;Не-Юнистрим;Текущее сальдо;текущий долг
");
                    foreach (string line in File.ReadLines(string.Format(@"d:\tmp\limit\{0}.log", date), Encoding.GetEncoding(1251)))
                    {
                        if (line.Contains("*$$$*"))
                        {
                            var matchCollection = regex.Matches(log.ToString());
                            if (matchCollection.Count > 0)
                            {
                                for (j = 1; j <= 10; j++)
                                {
                                    w.Write("{0};", matchCollection[0].Groups[j]);

                                }
                                w.WriteLine();
                                i++;
                                Console.Write("." );
                            }
                            log = new StringBuilder();
                            j++;
                        }
                        log.AppendLine(line);
                     
                    }
                }
            }
            Console.WriteLine(i);
        }
    }
}