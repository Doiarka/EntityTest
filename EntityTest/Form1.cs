using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AngleSharp;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using System.Net;

namespace EntityTest
{
    public partial class Form1 : Form
    {
        public List<string> categories = new List<string>(); //список категорий рефератов
        public List<Tuple<string, string>> catsHrefs = new List<Tuple<string, string>>(); // ссылка на реферат и категория
        List<Referat> refs = new List<Referat>(); //рефераты типа category, data

        public Form1()
        {
            InitializeComponent();
        }

        //Получить данные с Яндекса
        private void button1_Click(object sender, EventArgs e)
        {
            Int32 start = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; //начало парсинга
            AngleSharp(); //получение списка категорий

            try
            {
                int refscount = (int)ReferatsCountNumericUpDown.Value + 1; //сколько рефератов качать
                //для каждой категории
                foreach (var item in categories)
                {
                    //все рефераты
                    for (int i = 1; i < refscount; i++)
                    {
                        //создаем список ссылок на рефераты
                        catsHrefs.Add(new Tuple<string, string>("https://yandex.ru/referats/?t=" + item + "&s=" + i.ToString(), item));
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            //получение рефератов
            foreach (var t in catsHrefs)
            {
                //парсим страницу реферата
                AngleSharp2(t.Item1, t.Item2);
            }
            Int32 end = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; //конец парсинга
            MessageBox.Show("Рефераты с Яндекса загружены. Загрузка заняла: " + (end-start).ToString() + " секунд"); //сообщение об окончании парсинга
            InsertRefsToDB(); //вставляем данные в бд
        }

        //получение списка категорий
        public IEnumerable<string> AngleSharp()
        {
            WebClient dl = new WebClient();
            var html = dl.DownloadString("https://yandex.ru/referats/");
            var parser = new HtmlParser();
            var document = parser.Parse(html);
            
            foreach (IElement element in document.QuerySelectorAll("div.topics__item > span > span > input"))
            {
                categories.Add(element.GetAttribute("value"));
            }
            
            return categories;
        }

        //получение реферата
        public IEnumerable<string> AngleSharp2(string href, string cat)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var task = BrowsingContext.New(config).OpenAsync(href); //используем чтобы не было проблем с кодировкой
            var parsedHtml = task.Result;
            string data = null;
            //получаем текст реферата по абзацам и соединяем их
            foreach (IElement element in parsedHtml.QuerySelectorAll(".referats__text > p"))
            {
                data = data + element.TextContent + " "; //\r\n
            }
            Referat referat = new Referat();
            referat.Category = cat;
            referat.Data = data;
            refs.Add(referat); //добавляем реферат в список рефератов
            
            return categories;
        }

        //вставка данных в бд
        public void InsertRefsToDB()
        {
            using (MyModel db = new MyModel())
            {
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Referats]"); //очищаем таблицу в бд
                foreach (Referat item in refs)
                {
                    db.Referats.Add(item); //записываем данные в бд
                }
                db.SaveChanges(); //сохраняем изменения бд
            }
        }

        //топ 5 категорий по кол-ву слов в реферате
        public void ShowTOP5()
        {
            textBox1.Clear();

            using (MyModel db = new MyModel())
            {
                //получаем список рефератов вида category, data
                var count = db.Referats
                    .Select(x => new { x.Category, x.Data })
                    .ToList();

                //топ5
                var c2 = count
                    .GroupBy(d => d.Category) //группируем по категориям
                    .Select(
                        g => new
                        {
                            Category = g.First().Category,
                            Value = g.Sum(s => s.Data.Count(x => x == ' ')) //суммируем кол-во для каждого реферата
                        })
                        .OrderByDescending(x => x.Value) //сортируем по кол-ву
                        .Take(5) //берем первые 5
                        .ToList();

                foreach (var obj in c2)
                {
                    textBox1.Text += "Категория: " + obj.Category + ". Кол-во слов: " + obj.Value + "\r\n"; //отображаем на экране
                }
            }
        }

        //показать топ 5
        private void button2_Click(object sender, EventArgs e)
        {
            ShowTOP5();
        }
    }
}
