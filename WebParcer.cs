using System;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace LehaCovidBotVS
{
    internal class WebParcer
    {
        private static StringBuilder data = new StringBuilder();
        private static StringBuilder prettyDataToSend = new StringBuilder("Бот был только что запущен прошу подождие 1 минуту");
        private static DateTime dataNow;

        public static string GetUrlString(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = client.GetAsync(url);
                    if (response != null)
                    {
                        string[] words = response.Result.Content.ReadAsStringAsync().Result.Split('\n');
                        var index = words.Count();
                        return words[index - 2];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting info from URL :{url}", ex);
            }

            return null;
        }

        public static bool CheckForUpdate(string url)
        {
            dataNow = DateTime.Now.Date;
            string dataNowString = dataNow.ToString("yyyy-MM-dd");
            string lastData = Program.LastData;
            
            if (data == null) data = new StringBuilder();
            data.Clear();
            data.Append(GetUrlString(url));
            string nowData = data.ToString().Split(",").First();
            string nowDataOnlyData = nowData.Split(" ").First();

            //Console.WriteLine($"Datanow:{nowDataOnlyData} SystemData:{dataNowString} lats+now:{String.Equals(nowDataOnlyData, dataNowString)}");
            //Console.WriteLine($"last:{lastData}\nnow:{nowData} \nlats+now:{String.Equals(lastData, nowData)}");
            //Console.WriteLine($"{String.Equals(lastData, nowData) && !(String.Equals(lastData, nowData))}");

            if (String.Equals(lastData, nowData) && (String.Equals(lastData, nowData)))
            {
                Console.WriteLine("Данные все еще Старые");
            }
            else
            {
                Console.WriteLine("Обнаружены новые данные");
                Program.LastData = nowData;
                GetStringToPretty(data);
                return true;
            }

            return false;
        }

        //public static string GetCallUrlString(string url)
        //{
        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            var response = client.GetAsync(url);
        //            if (response != null)
        //            {
        //                string[] words = response.Result.Content.ReadAsStringAsync().Result.Split('\n');
        //                var index = words.Count();
        //                return GetStringToPretty(words[index - 2]);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting info from URL :{url}", ex);
        //    }

        //    return null;
        //}

        public static void GetStringToPretty(StringBuilder data)
        {
            string[] dataSplit = data.ToString().Split(',');
            prettyDataToSend.Clear();
            prettyDataToSend.Append($"Короновирус в СПб \n=====================\n{dataSplit[0].Trim()}\n\nЗа сегодня: <b>{dataSplit[1]}</b>\n=====================");
        }

        public static string SendPrettyData()
        {
            Console.WriteLine("Отправляем данные");
            return prettyDataToSend.ToString();
        }

        public static string RequestDataSend()
        {

            return SendPrettyData();
        }
    }
}