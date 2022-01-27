using System;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace LehaCovidBotVS
{
    internal class WebParcer
    {
        public static string GetCallUrlString(string url)
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
                        return GetStringToPretty(words[index - 2]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting info from URL :{url}", ex);
            }

            return null;
        }

        public static string GetStringToPretty(string data)
        {
            StringBuilder st = new StringBuilder();
            string[] dataSplit = data.Split(',');
            st.Append($"Короновирус в СПб \n=====================\n{dataSplit[0].Trim()}\n\nЗа сегодня: <b>{dataSplit[1]}</b>\n=====================");
            return st.ToString();
        }
    }
}