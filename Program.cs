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
        public static string LastData { get; set; }

        public static async Task SendGregMessageAsync(Exception ex)
        {
            await botClient.SendTextMessageAsync(145722603, $"АЛЯРМА У БОТА ПРОБЛЕМЫ:\n{ex.Message}\n{ex.StackTrace}", ParseMode.Html);
        }

        private static async Task Main(string[] args)
        {
            try
            {
                WebParcer.CheckForUpdate(url);
                Console.WriteLine("Start Program");
                botToken = Environment.GetEnvironmentVariable("Telegram");
                Console.WriteLine($"BotToken: {botToken}");

                path = @"/userData/UserId.txt";
                BDTextToList();
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
                //.WithIntervalInSeconds(1)
                //.RepeatForever())
                .WithCronSchedule("0 0/10 10-16 * * ?")
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
                        if (userId != null)
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
                        Console.WriteLine($"Error this send mes for scheduler: {ex.Message}\n{ex.StackTrace}");
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
                try
                {
                    if (msg.Text == "/start")
                    {
                        CheckNewId(msg.Chat.Id);
                    }else if (msg.Text != null)
                    {
                        SendMessage(msg.Chat.Id);
                    }
                    Console.WriteLine("Message send");
                }
                catch (Exception ex)
                {
                    await SendGregMessageAsync(ex);
                    Console.WriteLine("Error" + ex);
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
                if (WebParcer.CheckForUpdate(url))
                {
                    await botClient.SendTextMessageAsync(id, WebParcer.SendPrettyData(), ParseMode.Html);
                }
            }
        }

        private static async void SendMessage(long id)
        {
            if (id != null)
            {
                await botClient.SendTextMessageAsync(id, WebParcer.RequestDataSend(), ParseMode.Html);
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