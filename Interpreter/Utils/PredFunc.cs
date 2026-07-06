using Interpreter.CommonData;
using Interpreter.DataTemplates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Interpreter.Utils
{
    // /// что то описали
    internal class PredFunc
    {
        // /// словарь по имени модуля формируем список имен функций, определенных в модуле.
        //private static Dictionary<string, List<string>> dicNameFunc = new Dictionary<string, List<string>>(); 

        private static List<string> namefunc = new List<string>();      // список определяемых функций с возвратом
        private static List<string> listMod = new List<string>();       // список импортированных модулей 

        // /// пустой конструктор 
        public PredFunc()
        {
        }
        
        // /// метод для предпрохода по тексту с заменой определений функций с типом возвращаемого значения и их вызовов в выражениях 
        public List<string> Exec(string nameMod, string[] textIn, int priznak)      // nameMod - имя модуля (файла), textIn - исходный текст, priznak - 0 - bp, 1 - bpi, 2 - bpm
        {
            namefunc.Clear();                                                       // список определяемых функций с возвращаемым типом
            listMod.Clear();                                                        // список импортированных модулей
            List<string> text = new List<string>();                                 // новый текст после анализа определений функций
            string varNameFunc = "";                                                // текущее имя выходного параметра функции 
            foreach (string s in textIn)                                            // Находим определения функций
            {
                if (s == "") continue;                                              // пропускаем пустые строки.
                var k = s.IndexOf("'");
                var ss = s;
                if (k >= 0) ss = s.Remove(k);                                       // Убираем комментарии, чтобы не мешали
                var w = SplitWord(ss);
                if (w.Length == 0) continue;                                        // пропускаем пустые строки
                if (w[0].ToLower() == "function")                                   // нашли начальную строку определения функции
                {
                    string name = "";
                    if (w[1].ToLower() == "number" || w[1].ToLower() == "string")   // функция нового вида с заданным возвращаемым значением
                    {
                        if (w[2] != null && w[2].EndsWith("("))
                        {
                            name = w[2].Remove(w[2].Length - 1, 1);
                            namefunc.Add(w[2]);                                     // добавляем имя функции (вместе с "(") в список имен функций
                            varNameFunc = " r_" + name;                             // запоминаем имя выходной переменной, которой будем задавать возвращаемое значение в строке return
                            var indx = w[w.Length - 1].IndexOf(")");
                            w[w.Length - 1] = w[w.Length - 1].Substring(0, indx);
                            if (!(w.Length == 4 || w.Length == 5 && w[4] == "")) w[w.Length - 1] = w[w.Length - 1] + ", ";
                            w[w.Length - 1] = w[w.Length - 1] + "out " + w[1] + varNameFunc + ")";        // добавляем выходной формальный параметр
                            ss = w[0] + " " + w[2];                                       // выбрасываем тип возвращаемого значения
                            for (var i = 3; i < w.Length; i++) ss = ss + " " + w[i];       // пересобираем строку
                        }
                    }
                }
                else if (w[0].ToLower() == "return" && varNameFunc != "")           // разбираем return 
                {
                    ss = ""; for (var i = 1; i < w.Length; i++) ss += w[i];         // остаток строки после return - это возвращаемое значение (которое долдно быть)
                    ss = varNameFunc + " = " + ss;                                  // задаем возвращаемое значение выходной переменной "r_"имя функции 
                    text.Add(ss);                                                   // вставляем эту строку
                    ss = "return";                                                  // а затем return 
                }
                else if (w[0].ToLower() == "import")                                // разбираем импорт
                {
                    if (w.Length > 1)
                    {
                        w[1] = w[1].Trim('"');                                      // заносим имя импортированного модуля без '"'
                        listMod.Add(w[1]);                                          // в список импортированных модулей
                    }
                }
                text.Add(ss);
            }
            //dicNameFunc.Add(nameMod, namefunc);                                     // запоминаем в словаре имена функций с возвращаемым типом для разбираемого модуля
            //foreach (var n in listMod)                                              // если были импортированные модулм, то надо прибавить функции (методы) определенные в них в список функций 
            //{
            //    //if (dicNameFunc.ContainsKey(n))
            //    {
            //        foreach (var f in dicNameFunc[n])                               // 
            //        {
            //            namefunc.Add(n + '.' + f);                                  // к имени функции добавляем имя модуля через '.'
            //        }
            //    }
            //}                          
            List<string> textOut = new List<string>();                              // выходной текст после замены вызовов функций
            List<string> callnamefunc = new List<string>();                         // Список вызовов функций в одной строке (чтобы сделать для одной функции в одной строке несколько вызовов)
            //List<string> snamestek = new List<string>();                            // Список вставляемых строк с вызовами одноименной функции в одной строке
            var lineIn = 0;
            foreach (string s1 in text)                                             // Находим вызовы функций
            {
                lineIn++;
                //var k = s1.IndexOf("'");
                var ss = s1;
                //if (k >= 0) ss = s1.Remove(k);                                      // Убираем комментарии, чтобы не мешали
                string s2 = "";
                string sname = "";
                callnamefunc.Clear();                                               // в этой строке пока нет вызовов функций
                string suffunc = "";                                                // суффикс для повторного вызова одной функции
                ss = AddSpace(ss, new char[] { ')', '-', '+', '*', '/' });          // добавляем пробелы перед ) и знаками арифметических операций
                var w = SplitWord(ss);                                              // разбираем строку на слова
                if (w.Length == 0) continue;                                        // пропускаем пустые строки
                if (w[0].ToLower() != "function")                                   // Если строка не определение функции - значит может содержать вызов функции или метода
                {
                    for (var i = 0; i < w.Length; i++)                                  // пересобираем строку
                    {
                        var isMetod = false;
                        int k = w[i].IndexOf(".");                                          // для проверки вызовов методов импортированных модулей
                        if ((k >= 0) && listMod.Contains(w[i].Substring(0, k)))
                        {
                            isMetod = true;
                            if ((i > 0) && w[i].IndexOf("(") >= 0)                   // только для методов с возвращаемым значением (вызов такого метода не может быть первым словом)
                            {
                                namefunc.Add(w[i]);  // добавляем в список определенных в модуле функций вызываемый метод импортированного модуля (если его там еще не было и если он не первый в строке)
                            }
                        }
                        // Ищем вызов функции или вызов метода импортированного модуля
                        if (namefunc.Contains(w[i]))                                    // Ищем вызовы определенных в файле функций
                        {                                                               // Нашли вызов функции
                            sname = w[i];                                               // формируем предыдущую строку с вызовом функции
                            if (callnamefunc.Contains(sname))                           // проверяем на повторный вызов одной и той же функции в одной строке
                            {
                                // повторный вызов функции в одной строке - число вхождений - есть суффикс для параметров.
                                suffunc = "_" + callnamefunc.FindAll(s => s == sname).Count.ToString();

                            }
                            callnamefunc.Add(sname);                                   // запоминаем имя вызываемой функции

                            if (isMetod) w[i] = w[i].Replace('.', '_');                 // для метода в имени переменной заменяем точку на "_"
                            varNameFunc = "f_" + w[i].Remove(w[i].Length - 1, 1) + suffunc;   // имя переменной с префиксом "f_"имяфункции без "(" + суффикс повторного вызова
                            s2 = s2 + varNameFunc + " ";                                // вставляем в текущую строку вместо вызова функции.                
                            for (int j = i + 1; j < w.Length; j++)                      // параметры функции в скобках (фактические значения) все аргументы до слова с последним символом ")"
                            {                                                           // переносим в sname
                                bool isParam = false;                                   // вызов функции как параметр внешней функции
                                if (namefunc.Contains(w[j])) Data.Errors.Add(new Errore(lineIn, nameMod, 1037, w[j]));  // пока нельзя вызывать функцию в качетсве фактического параметра функции 
                                if (w[j].EndsWith("))"))                                // Вызов функции в качестве последнего параметра внешней функции
                                {
                                    if (w[j].EndsWith("())")) w[j] = w[j].Remove(w[j].Length - 2, 1); // для вызова без парметров
                                    w[j] = w[j].Remove(w[j].Length - 1);                // убираем лишнюю )
                                    isParam = true;                                     // задаем признак, что это вызов функции без параметров или параметр внешней функции
                                }
                                if (w[j].EndsWith(")"))                                 // нашли последний параметр в вызове функции
                                {
                                    // добавляем фактический параметр - переменную с префиксом "f_" и именем функкции в качестве возвращаемого значения функции в конец списка параметров функции
                                    sname = sname + w[j].Remove(w[j].Length - 1, 1);
                                    if (w[j].IndexOf("))") > 0) sname = sname.Remove(sname.Length - 1, 1); // убираем лишнюю ) в случае завершения слова на ))
                                    if (w[j].Length > 1 || j - i > 1) sname = sname + ", ";
                                    sname = sname + varNameFunc + ")";
                                    textOut.Add(sname);                                 // вставляем вызов функции в предыдущую строку 

                                    sname = "";                                         // очищаем накопитель строки с вызовом функции
                                    if (w[j].IndexOf("))") > 0) s2 = s2 + ")";          // не пропускаем ) в случае завершения слова на ))
                                    if (isParam) s2 = s2 + ")";                         // а также вызов функции как параметр
                                    i = j;                                              // продолжаем разбор текущей строки со следующего слова
                                    break;                                              // в предыдущем цикле по i
                                }
                                else sname = sname + w[j] + " ";                        // формируем строку вызова функции - добавляем очередной параметр.
                            }
                        }
                        else s2 = s2 + w[i] + " ";                                      // добавляем в строку очередное слово
                    }
                }
                else s2 = ss;
                textOut.Add(s2);                                                        // Перекинули текущую строку в выходной текст
            }
            return textOut;
        }
       
        internal string[] SplitWord(string s)
        // Разборка строки на слова с сохранением "(", ")", ","
        {
            //s = AddSpace(s, new char[] { ')', '-', '+', '*', '/' });
            var w = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> w2 = new List<string>();
            foreach (var w1 in w)
            {
                var ind = w1.IndexOf('(', 0);
                if (ind > 0)
                {
                    w2.Add(w1.Substring(0, ind + 1));
                    if (ind + 1 < w1.Length) w2.Add(w1.Substring(ind + 1));
                }
                else w2.Add(w1);
            }
            return w2.ToArray();
        }

        internal string AddSpace(string s, char[] c)
        // вставка в строку s пробела перед каждым символом из списка c
        {
            string ss = "";
            int l = s.IndexOfAny(c);
            while (l >= 0)
            {
                ss = ss + s.Substring(0, l);
                if (ss[ss.Length-1] != ' ') ss = ss + " ";
                ss = ss + s[l];
                s = s.Substring(l+1);
                l = s.IndexOfAny(c);
            }
            ss = ss + s;
            return ss;
        }
    }
}