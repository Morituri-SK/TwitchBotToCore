using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
//using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;


namespace SimpleTwitchBot
{
    public static class Globals
    {
        public static Boolean CanSpawn = false; // Modifiable
    }
    class Program
    {
        // import the function in your class
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        //...
        static async Task Main(string[] args)
        {
            //config read file (no check)
            string[] lines = File.ReadAllLines("config.txt"); //no foolproofing, you have to have the config.txt right
            string password = lines[0];
            string botUsername = lines[1];
            var twitchBot = new TwitchBot(botUsername, password);
            twitchBot.Start().SafeFireAndForget();
            //We could .SafeFireAndForget() these two calls if we want to
            string channelName = lines[2];
            await twitchBot.JoinChannel(channelName);
            await twitchBot.SendMessage(channelName, "Deathdrop Chatbot is ready!");
            string ModeratorName = lines[3];
            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                Console.WriteLine($"{twitchChatMessage.Sender} said '{twitchChatMessage.Message}'");
                //Listen for !hey command
                if (twitchChatMessage.Message.StartsWith("!play"))
                {
                    if (Globals.CanSpawn)
                    {
                        var SenderName = twitchChatMessage.Sender;
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"Let me spawn you, {SenderName}");

                        Process p = Process.GetProcessesByName("Platform-Win64-Shipping").FirstOrDefault(); //this strange name is a coregames client process
                        if (p != null) //TODO this could need a timed feeder, to prevent the keystrokes mismatching, if the need arises
                        {
                            IntPtr h = p.MainWindowHandle;
                            SetForegroundWindow(h);
                            //SendKeys.SendWait("~");
                            SendKeys.SendWait($"~/spawn {SenderName}~");
                            //SendKeys.SendWait("~");
                        }
                    }
                    else
                    {
                        var SenderName = twitchChatMessage.Sender;
                        await twitchBot.SendMessage(twitchChatMessage.Channel, $"Wait for the moderator to allow spawning, {SenderName}!");
                    }

                }
                else if (twitchChatMessage.Message.StartsWith("!deathdrop"))
                {
                    var SenderName = twitchChatMessage.Sender;
                    if (SenderName == ModeratorName)
                    {
                        if (!Globals.CanSpawn)
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "GAME IS OPEN, TYPE !play TO JOIN");
                            Thread.Sleep(1000);
                            Globals.CanSpawn = true;
                        }
                        else
                        {
                            await twitchBot.SendMessage(twitchChatMessage.Channel, "GAME IS NOW CLOSED FOR NEW PLAYERS, WAIT FOR THE NEXT OPPORTUNITY.");
                            Thread.Sleep(1000);
                            Globals.CanSpawn = false;
                        }
                    }

                }
            };

            await Task.Delay(-1);
        }
    }
}