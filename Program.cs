using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace LehaCovidBotVS
{
    internal class Program
    {
        private static TelegramBotClient? botClient;
        public static string url = "https://raw.githubusercontent.com/alexei-kouprianov/COVID-19.SPb.monitoring/main/data/SPb.stopkoronavirus_archived.csv";
        private static List<long> userId;
        private static string pathIdBD;
        private static string pathData;
        private static string botToken;
        private static string versionProg;
        public static string LastData { get; set; }

        public static async Task SendGregMessageAsync(Exception ex)
        {
            await botClient.SendTextMessageAsync(145722603, $"АЛЯРМА У БОТА ПРОБЛЕМЫ:\n{ex.Message}\n{ex.StackTrace}", ParseMode.Html);
        }

        public static async Task SendGregMessageAsync(String Text)
        {
            await botClient.SendTextMessageAsync(145722603, $"Версия {Text.ToString()}, ParseMode.Html");
        }

        private static async Task Main(string[] args)
        {
            try
            {
                //WebParcer.CheckForUpdate(url);
                Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || Start Program");
                botToken = Environment.GetEnvironmentVariable("Telegram");
                Console.WriteLine($"BotToken: {botToken}");

                pathIdBD = @"/userData/UserId.txt";
                pathData = @"/userData/Data.txt";
                BDTextToList();
                BDText();


                BotStart();

                
            }
            catch (Exception ex)
            {
                await SendGregMessageAsync(ex);
                Console.WriteLine($"Ощибки при запуске {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async Task TestScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
            IJobDetail job = JobBuilder.Create<EveryDaySendJob>()
                    .WithIdentity("send covid job", "send covid group")
                    .Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("send covid trigger", "send covid group")
                //.WithSimpleSchedule(x => x
                //.WithIntervalInSeconds(10)
                //.RepeatForever())
                .WithCronSchedule("0 0/10 10-19 * * ?")
                .Build();
            await scheduler.ScheduleJob(job, trigger);
         
        }

        public class EveryDaySendJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                try
                {
                    try
                    {
                        if (userId != null && WebParcer.CheckForUpdate(url))
                        {
                            foreach (long user in userId)
                            {
                                SendMessageEveryDay(user);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await SendGregMessageAsync(ex);
                        Console.WriteLine($"Error this send mes for scheduler: {ex.Message}\n{ex.StackTrace} ");
                    }
                }
                catch (Exception ex)
                {
                    await SendGregMessageAsync(ex);
                }

                await Task.CompletedTask;
            }
        }

        private static async Task BotStart()
        {
            try 
            {
                botClient = new TelegramBotClient(botToken);
                botClient.StartReceiving();
                botClient.OnMessage += OnMessageHandler;

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                versionProg = fvi.FileVersion;
                Console.WriteLine($"Старт версии {versionProg}");
                versionProg = $"Версия: {versionProg} \nСделано с любовью 🤔?\n∠( ᐛ 」∠)＿ Конешно дорохой!";

                await TestScheduler();
                Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || А ЗЕМЛЯ ТО ПЛОСКАЯ! КОНЕЦЦЦЦЦЦЦЦЦЦЦЦЦ");
                
                Thread.Sleep(Timeout.Infinite);
                botClient.StopReceiving();
            }
            catch (Exception ex)
            {
                Console.WriteLine("!!!!!Error!!!!!! Проблемы со стартом" + ex);
                await SendGregMessageAsync(ex);
            }
            
        }

        private static void BDTextToList()
        {
            if (!File.Exists(pathIdBD))
            {
                Console.WriteLine($"Create txt file. Path: {pathIdBD}");
                using (StreamWriter sw = File.CreateText(pathIdBD)) { }
            }
            else
            {
                Console.WriteLine($"Find txt file. Path: {pathIdBD}");
            }

            using (StreamReader sr = File.OpenText(pathIdBD))
            {
                string[] readText = File.ReadAllLines(pathIdBD);
                if (readText.Length != 0)
                {
                    userId = new List<long>();
                    foreach (string line in readText)
                    {
                        try
                        {
                            if (line != "")
                            {
                                userId.Add(long.Parse(line));
                            }
                        }
                        catch
                        {
                            Console.WriteLine("!!!!!!Error!!!!!! txt to list ");
                        }
                    }
                }
            }
        }

        private static void BDText()
        {
            if (!File.Exists(pathData))
            {
                Console.WriteLine($"Create txt file. Path: {pathData}");
                using (StreamWriter sw = File.CreateText(pathData)) { }
            }
            else
            {
                Console.WriteLine($"Find txt file. Path: {pathData}");
            }

            using (StreamReader sr = File.OpenText(pathData))
            {
                string readText = File.ReadAllText(pathData);
                Console.WriteLine(readText.Length);
                if (readText.Length != 0)
                {
                    LastData = readText;         
                    Console.WriteLine($"Old data. data: {LastData}");
                }
                else
                {
                    Console.WriteLine($"Нету данных о ковиде в базе");
                }               
            }
        }

        private static async void OnMessageHandler(object sender, MessageEventArgs e)
        {            
            var msg = e.Message;
            Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || message from {msg.Chat.Id}");
            if (msg.Text != null)
            {
                try
                {
                    if (msg.Text == "/start")
                    {
                        CheckNewId(msg.Chat.Id);
                    }
                    else if(msg.Text == "V")
                    {
                        SendVersionMessage(msg.Chat.Id);
                    }
                    else if (msg.Text != null)
                    {
                        SendMessage(msg.Chat.Id);
                    }

                    Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || Message send");
                }
                catch (Exception ex)
                {
                    await SendGregMessageAsync(ex);
                    Console.WriteLine("!!!!!Error!!!!!!  Проблемы с OnMessageHandler" + ex);
                }

                //try
                //{
                //    SendMessage(msg.Chat.Id);
                //}
                //catch (Exception ex)
                //{
                //    await SendGregMessageAsync(ex);
                //    Console.WriteLine("Error" + ex);
                //}
            }
        }

        private static async void SendMessageEveryDay(long id)
        {
            if (id != null)
            {
                try
                {
                    await botClient.SendTextMessageAsync(id, WebParcer.SendPrettyData(), ParseMode.Html);
                }
                catch (Exception ex)
                {
                    await SendGregMessageAsync(ex);
                    Console.WriteLine("!!!!!Error!!!!!!  Проблемы с SendMessageEveryDay" + ex);
                }
                
            }
        }

        private static async void SendMessage(long id)
        {
            try
            {
                if (id != null)
                {
                    await botClient.SendTextMessageAsync(id, WebParcer.RequestDataSend(), ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                await SendGregMessageAsync(ex);
                Console.WriteLine("!!!!!Error!!!!!! Проблемы с SendMessage" + ex);
            }
           
        }

        private static async void SendVersionMessage(long id)
        {
            try
            {
                if (id != null)
                {
                    await botClient.SendTextMessageAsync(id, versionProg);
                }
            }
            catch (Exception ex)
            {
                await SendGregMessageAsync(ex);
                Console.WriteLine("!!!!!Error!!!!!! Проблемы с SendVersionMessage" + ex);
            }

        }

        private static void CheckNewId(long id)
        {
            bool isNewId = false;

            if (userId != null)
            {
                foreach (long userBD in userId)
                {
                    if (userBD == id)
                    {
                        isNewId = false;
                        Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || User id: {userBD}");
                        botClient.SendTextMessageAsync(id, "Пользователь уже <b>добавлен</b>", ParseMode.Html);
                        break;
                    }
                    else
                    {
                        isNewId = true;
                    }
                }
            }
            else
            {
                botClient.SendTextMessageAsync(id, "Новый пользователь <b>добавлен</b>", ParseMode.Html);
                userId = new List<long>();
                AddNewUser(id);
            }

            if (isNewId)
            {
                botClient.SendTextMessageAsync(id, "Новый пользователь <b>добавлен</b>", ParseMode.Html);
                AddNewUser(id);
            }
        }

        private static void AddNewUser(long id)
        {
            userId.Add(id);

            string idToTxt = $"\r\n{id.ToString()}";
            File.AppendAllText(pathIdBD, idToTxt, Encoding.UTF8);
            Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || Add new User: {id}");
        }

        public static void AddDataCovid(string data)
        {

            LastData = data;
            File.WriteAllText(pathData, data, Encoding.UTF8);
            Console.WriteLine($"{ DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") } || Add new data: {data}");
        }
    }
}