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
        private static string url = "https://raw.githubusercontent.com/alexei-kouprianov/COVID-19.SPb.monitoring/main/data/SPb.stopkoronavirus_archived.csv";
        private static List<long> userId;
        private static string path;
        private static string botToken;
        private static DateTime LastData;

        private static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Start Program");
                botToken = Environment.GetEnvironmentVariable("Telegram");
                Console.WriteLine($"BotToken: {botToken}");

                path = @"/userData/UserId.txt";
                BDTextToList();
                BotStart();
            }
            catch (Exception ex)
            {
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
                .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(3)
                //.WithIntervalInMinutes(1)
                .RepeatForever())
                //.WithCronSchedule("1 0 13-18 * * ?")
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
                        Console.WriteLine($"Time send mes: {DateTime.Now}");
                        foreach (long user in userId)
                        {
                            SendMessage(user);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error this send mes for scheduler: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Ощибки при проверки даты {ex.Message}\n{ex.StackTrace}");
                }
                

                await Task.CompletedTask;
            }
        }

        private static async Task BotStart()
        {
            botClient = new TelegramBotClient(botToken);
            botClient.StartReceiving();
            botClient.OnMessage += OnMessageHandler;
            await TestScheduler();
            Console.WriteLine("А ЗЕМЛЯ ТО ПЛОСКАЯ! КОНЕЦЦЦЦЦЦЦЦЦЦЦЦЦ");
            Thread.Sleep(Timeout.Infinite);
            botClient.StopReceiving();
        }

        private static void BDTextToList()
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Create txt file. Path: {path}");
                using (StreamWriter sw = File.CreateText(path)) { }
            }
            else
            {
                Console.WriteLine($"Find txt file. Path: {path}");
            }

            using (StreamReader sr = File.OpenText(path))
            {
                string[] readText = File.ReadAllLines(path);
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
                            Console.WriteLine("Error txt to list");
                        }
                    }
                }
            }
        }

        private static async void OnMessageHandler(object sender, MessageEventArgs e)
        {
            Console.WriteLine("Message");
            var msg = e.Message;
            if (msg.Text != null)
            {
                if (msg.Text == "/start")
                {
                    CheckNewId(msg.Chat.Id);
                }
                Console.WriteLine("Message send");

                try
                {
                    SendMessage(msg.Chat.Id);
                    //await botClient.SendTextMessageAsync(msg.Chat.Id, WebParcer.GetCallUrlString(url), ParseMode.Html);
                }
                catch (InvalidCastException error)
                {
                    Console.WriteLine("Error" + error);
                }
            }
        }

        private static async void SendMessage(long id)
        {
            if (id != null)
            {
                await botClient.SendTextMessageAsync(id, WebParcer.GetCallUrlString(url), ParseMode.Html);
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
                        Console.WriteLine($"User id: {userBD}");
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
            File.AppendAllText(path, idToTxt, Encoding.UTF8);
            Console.WriteLine($"Add new User: {id}");
        }
    }
}