﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices; // DllImport
using System.Diagnostics; // Process
using System.IO;
using System.Runtime.Remoting;

namespace compLexity_Demo_Player
{
    public class Program
    {
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        static readonly String ipcPortName = "coldemoplayerchannel";
        static readonly String ipcServername = "coldemoplayerserver";
        static public IMainWindow MainWindowInterface { get; set; }

        [STAThread]
        public static void Main(String[] args)
        {
            // read config.xml
            try
            {
                Config.Read();
            }
            catch (Exception ex)
            {
                Common.Message(null, "Error reading from the configuration file. Try deleting \"config.xml\" from  \"" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + Config.ProgramName + "\".", ex, MessageWindow.Flags.Error);
                return;
            }

            // look for already running process with same name and path
            Process process = Common.FindProcess(Path.GetFileNameWithoutExtension(Config.ProgramExeFullPath), Config.ProgramExeFullPath, Process.GetCurrentProcess().Id);

            if (process != null)
            {
                // focus on process window
                SetForegroundWindow(process.MainWindowHandle);

                if (args.Length > 0)
                {
                    if (File.Exists(args[0]))
                    {
                        // send command line parameter to existing process
                        try
                        {
                            CreateClientChannel(args[0]);
                        }
                        catch (Exception ex)
                        {
                            Common.Message(null, "Error creating client IPC channel.", ex, MessageWindow.Flags.Error);
                        }
                    }
                }

                // another process already exists, end this one
                return;
            }
            else
            {
                try
                {
                    CreateServerChannel();
                }
                catch (Exception ex)
                {
                    Common.Message(null, "Error creating server IPC channel.", ex, MessageWindow.Flags.Error);
                    return; // quit
                }

                AllowSetForegroundWindow(Process.GetCurrentProcess().Id);
            }

            // read steam.xml
            try
            {
                GameManager.Initialise(Config.ProgramPath + "\\config");
            }
            catch (Exception ex)
            {
                Common.Message(null, "Error reading from \"steam.json\". Reinstalling may fix the problem.", ex, MessageWindow.Flags.Error);
                return;
            }

            // read fileoperationslog.xml
            try
            {
                FileOperationList.Initialise(Config.ProgramDataPath, "fileoperationslog.xml");
                FileOperationList.Execute();
            }
            catch (Exception ex)
            {
                Common.Message(null, "FileOperationList error. Try deleting \"fileoperationslog.xml\" from \"" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + Config.ProgramName + "\" if the problem persists.", ex, MessageWindow.Flags.Error);
                return;
            }

            // file association
            if (Config.Settings.AssociateWithDemFiles)
            {
                try
                {
                    Config.AssociateWithDemFiles();
                }
                catch (UnauthorizedAccessException)
                {
                    Config.Settings.AssociateWithDemFiles = false;
                }
                catch (Exception ex)
                {
                    Common.Message(null, "Error associating with *.dem files.", ex, MessageWindow.Flags.Error);
                }
            }

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void CreateServerChannel()
        {
            IpcServerChannel channel = new IpcServerChannel(ipcPortName);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(Ipc), ipcServername, WellKnownObjectMode.Singleton);
        }

        private static void CreateClientChannel(String arg)
        {
            IpcClientChannel channel = new IpcClientChannel();
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownClientType(typeof(Ipc), "ipc://" + ipcPortName + "/" + ipcServername);

            Ipc ipc = new Ipc();
            ipc.OpenFile(arg);
        }

        public static void OpenFile(String fileName)
        {
            MainWindowInterface.OpenDemo(fileName);
        }
    }

    public class Ipc : MarshalByRefObject
    {
        public void OpenFile(String fileName)
        {
            Program.OpenFile(fileName);
        }
    }
}
