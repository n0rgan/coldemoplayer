﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics; // Process
using System.Windows;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using hlae.remoting;
using System.Runtime.InteropServices;

namespace compLexity_Demo_Player
{
    public abstract class Launcher
    {
        public class AbortLaunchException : Exception { }

        public Boolean UseHlae { get; set; }
        public Demo Demo { get; set; }

        public Window Owner { get; set; } // needed for a blocking message window
        public ILauncher Interface { get; set; }
        public String GameFullPath
        {
            get
            {
                return gameFullPath;
            }
        }
        protected String gameFullPath;
        protected String processExeFullPath;
        protected String launchParameters; // FIXME: stored, because they need to be deferred until the remote connection is made to HLAE
        private Thread processMonitorThread;

        /// <remarks>gameFullPath needs to be set here/</remarks>
        protected abstract void VerifyProgramSettings();
        /// <remarks>processExeFullPath needs to be set here.</remarks>
        protected abstract void VerifyRunningPrograms();
        protected virtual void PreLaunch() { }
        protected abstract void LaunchProgram();

        protected void CheckMapVersion()
        {
            // FIXME:
            // not really needed, just don't call this
            // remove me when source map checksum stuff is figured out
            if (Demo.Engine == Demo.Engines.Source)
            {
                return;
            }

            // see if the game has a config file
            // FIXME: engine name - assuming goldsrc for now (see above)
            Game game = GameManager.Find(Demo);

            if (game == null || game.HasConfig == false)
            {
                // can't determine what maps are built-in for the game - can't handle map versions
                return;
            }

            // see if the map is built-in
            if (game.BuiltInMapExists(Demo.MapChecksum, Demo.MapName))
            {
                // ok, suitable map found
                return;
            }

            // see if the map already exists in the game's "maps" folder
            String mapDestinationPath = gameFullPath + "\\maps";
            String mapDestinationFileName = mapDestinationPath + "\\" + Demo.MapName + ".bsp";

            if (File.Exists(mapDestinationFileName))
            {
                if (HalfLifeMapChecksum.Calculate(mapDestinationFileName) == Demo.MapChecksum)
                {
                    // ok, suitable map found
                    return;
                }
            }

            // see if we have a map in the map pool matching the checksum
            String mapSourcePath = Config.ProgramDataPath + "\\maps\\goldsrc\\" + String.Format("{0}\\{1}", Demo.GameFolderName, Demo.MapChecksum);
            String mapSourceFileName = mapSourcePath + "\\" + Demo.MapName + ".bsp";

            if (!File.Exists(mapSourceFileName))
            {
                DownloadMapWindow downloadMapWindow = new DownloadMapWindow(Demo.MapName, Config.MapsUrl + "goldsrc/" + String.Format("{0}/{1}_{2}.zip", Demo.GameFolderName, Demo.MapChecksum, Demo.MapName), mapSourcePath + "\\" + Demo.MapName + ".zip");
                downloadMapWindow.Owner = Owner;

                if (downloadMapWindow.ShowDialog() == false)
                {
                    // can't find a suitable map
                    if (Common.Message(Owner, String.Format("Can't find a version of \"{0}\" with a matching checksum \"{1}\" for this demo. Continue with playback anyway?", Demo.MapName, Demo.MapChecksum), null, MessageWindow.Flags.ContinueAbort) != MessageWindow.Result.Continue)
                    {
                        throw new AbortLaunchException();
                    }

                    return;
                }
            }

            // first make sure the desination "maps" folder exists (create it if it doesn't)
            if (!Directory.Exists(mapDestinationPath))
            {
                Directory.CreateDirectory(mapDestinationPath);
            }

            // if original map exists, rename to *.bak (if that exists, just delete it)
            String backupDestinationFileName = Path.ChangeExtension(mapDestinationFileName, ".bak");

            if (File.Exists(mapDestinationFileName))
            {
                if (File.Exists(backupDestinationFileName))
                {
                    File.Delete(backupDestinationFileName);
                }

                File.Move(mapDestinationFileName, backupDestinationFileName);
                FileOperationList.Add(new FileMoveOperation(backupDestinationFileName, mapDestinationFileName));
            }

            // copy old version of map
            File.Copy(mapSourceFileName, mapDestinationFileName);
            FileOperationList.Add(new FileDeleteOperation(mapDestinationFileName));

            // copy any wad files (if they don't already exist)
            DirectoryInfo directoryInfo = new DirectoryInfo(mapSourcePath);

            foreach (FileInfo fi in directoryInfo.GetFiles("*.wad", SearchOption.TopDirectoryOnly))
            {
                // wad destination = demo destination
                String wadDestinationFileName = gameFullPath + "\\" + fi.Name;

                if (!File.Exists(wadDestinationFileName))
                {
                    File.Copy(fi.FullName, wadDestinationFileName);
                }
            }
        }

        public void Verify()
        {
            VerifyProgramSettings();
            Debug.Assert(gameFullPath != null);
            VerifyRunningPrograms();
            Debug.Assert(processExeFullPath != null);

            try
            {
                PreLaunch();
            }
            catch (Exception)
            {
                try
                {
                    FileOperationList.Execute();
                }
                catch (Exception)
                {
                    // absorb any post-launch errors in this case, since we're just doing clean up
                }

                throw;
            }
        }

        public void Run()
        {
            WriteConfig();
            LaunchProgram();

            // start process monitor thread
            processMonitorThread = new Thread(new ThreadStart(ProcessMonitorThread));
            processMonitorThread.Start();
        }

        public void AbortProcessMonitor()
        {
            Common.AbortThread(processMonitorThread);
        }

        private void ProcessMonitorThread()
        {
            Process process = null;

            while (true)
            {
                if (process == null)
                {
                    // looking for process
                    process = Common.FindProcess(System.IO.Path.GetFileNameWithoutExtension(processExeFullPath), processExeFullPath);

                    if (process != null)
                    {
                        Interface.LaunchedProcessFound();

                        // set process priority
                        if (!UseHlae && Config.Settings.GameProcessPriority != ProcessPriorityClass.Normal)
                        {
                            process.PriorityClass = Config.Settings.GameProcessPriority;
                        }

                        if (UseHlae)
                        {
                            // open IPC channel
                            IpcChannel channel = new IpcChannel();
                            ChannelServices.RegisterChannel(channel, true);

                            try
                            {
                                IHlaeRemote_1 remote = (IHlaeRemote_1)Activator.GetObject(typeof(IHlaeRemote_1), "ipc://localhost:31337/Hlae.Remote.1");

                                // bleh
                                Boolean connected = false;
                                Int32 attempts = 0;

                                do
                                {
                                    try
                                    {
                                        connected = true;
                                        remote.GetCustomArgs();
                                    }
                                    catch (RemotingException)
                                    {
                                        connected = false;
                                        attempts++;
                                        Thread.Sleep(50);
                                    }
                                }
                                while (!connected && attempts < 10);

                                if (!connected)
                                {
                                    MessageBox.Show("Failed to communicate with the HLAE process remotely. You must manually launch the game and enter the command \"exec coldemoplayer.cfg\" in the console.", Config.ProgramName);
                                }
                                else
                                {
                                    if (remote.IsDeprecated())
                                    {
                                        MessageBox.Show("Warning: HLAE interface depreciated.");
                                    }

                                    try
                                    {
                                        remote.LaunchEx(remote.GetCustomArgs() + launchParameters);
                                    }
                                    catch (Exception e)
                                    {
                                        MessageBox.Show("Warning: " + e.Message, "IPC error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    }
                                }
                            }
                            finally
                            {
                                ChannelServices.UnregisterChannel(channel);
                            }
                        }
                    }
                }
                else
                {
                    // found process, waiting for it to close
                    if (process.HasExited)
                    {
                        Interface.LaunchedProcessClosed();
                        return;
                    }
                }

                Thread.Sleep(250);
            }
        }

        private void WriteConfig()
        {
            String configFileFullPath = gameFullPath;

            if (Demo.Engine == Demo.Engines.Source)
            {
                configFileFullPath += "\\cfg";
            }

            configFileFullPath += "\\" + Config.LaunchConfigFileName;
            StreamWriter stream = File.CreateText(configFileFullPath);

            try
            {
                if (Demo.Engine == Demo.Engines.Source)
                {
                    stream.WriteLine("alias +col_ff_slow \"demo_timescale 2\"");
                    stream.WriteLine("alias -col_ff_slow \"demo_timescale 1\"");
                    stream.WriteLine("alias +col_ff_fast \"demo_timescale 4\"");
                    stream.WriteLine("alias -col_ff_fast \"demo_timescale 1\"");
                    stream.WriteLine("alias +col_slowmo \"demo_timescale 0.25\"");
                    stream.WriteLine("alias -col_slowmo \"demo_timescale 1\"");
                    stream.WriteLine("alias col_pause demo_togglepause ");
                }
                else
                {
                    stream.WriteLine("alias +col_ff_slow \"host_framerate 0.01; alias col_pause col_pause1\"");
                    stream.WriteLine("alias -col_ff_slow \"host_framerate 0\"");
                    stream.WriteLine("alias +col_ff_fast \"host_framerate 0.1; alias col_pause col_pause1\"");
                    stream.WriteLine("alias -col_ff_fast \"host_framerate 0\"");
                    stream.WriteLine("alias +col_slowmo \"host_framerate 0.001; alias col_pause col_pause1\"");
                    stream.WriteLine("alias -col_slowmo \"host_framerate 0\"");
                    stream.WriteLine("alias col_pause1 \"host_framerate 0.000000001; alias col_pause col_pause2\"");
                    stream.WriteLine("alias col_pause2 \"host_framerate 0; alias col_pause col_pause1\"");
                    stream.WriteLine("alias col_pause col_pause1\n");

                    stream.WriteLine("alias wait10 \"wait; wait; wait; wait; wait; wait; wait; wait; wait; wait\"\n");
                    stream.WriteLine("brightness 2");
                    stream.WriteLine("gamma 3");
                    stream.WriteLine("wait10");

                    if (Demo.Engine == Demo.Engines.HalfLifeSteam || ((HalfLifeDemo)Demo).ConvertNetworkProtocol())
                    {
                        stream.WriteLine("sv_voicecodec voice_speex");
                        stream.WriteLine("sv_voicequality 5");
                    }

                    stream.WriteLine("wait10");
                    stream.WriteLine("wait10");
                    stream.WriteLine("wait10");
                    stream.WriteLine("wait10");
                    stream.WriteLine("wait10");
                }

                String playbackType;

                if (Demo.Engine != Demo.Engines.Source)
                {
                    playbackType = (Config.Settings.PlaybackType == ProgramSettings.Playback.Playdemo ? "playdemo" : "viewdemo");
                }
                else
                {
                    playbackType = "playdemo";
                }

                stream.WriteLine("{0} {1}", playbackType, Config.LaunchDemoFileName);

                if (Demo.Engine != Demo.Engines.Source)
                {
                    stream.WriteLine("wait10");
                    stream.WriteLine("slot1");

                    // TODO: remove this, figure out why the spec menu it's initalised correctly with old converted HLTV demos.
                    if (Demo.Perspective == Demo.Perspectives.Hltv)
                    {
                        stream.WriteLine("wait10");
                        stream.WriteLine("spec_menu 0");
                        stream.WriteLine("wait10");
                        stream.WriteLine("spec_mode 4");
                        stream.WriteLine("wait10");
                        stream.WriteLine("wait10");
                        stream.WriteLine("wait10");
                        stream.WriteLine("spec_menu 1");
                        stream.WriteLine("wait10");
                        stream.WriteLine("+attack");
                    }
                }

                stream.WriteLine("echo \"\"");
                stream.WriteLine("echo \"==========================\"");
                stream.WriteLine("echo \"{0}\"", Config.ProgramName);
                stream.WriteLine("echo \"==========================\"");
                stream.WriteLine("echo \"Aliases:\"");
                stream.WriteLine("echo \"  +col_ff_slow (Fast Forward)\"");
                stream.WriteLine("echo \"  +col_ff_fast (Faster Fast Forward)\"");
                stream.WriteLine("echo \"  +col_slowmo (Slow Motion)\"");
                stream.WriteLine("echo \"  col_pause (Toggle Pause)\"");
                stream.WriteLine("echo \"\"");
                stream.WriteLine("echo \"Playing \'{0}\'...\"", Demo.Name);
                stream.WriteLine("echo \"  Duration: {0}\"", Common.DurationString(Demo.DurationInSeconds));
                stream.WriteLine("echo \"  Recorded by: {0}\"", Demo.RecorderName);
                stream.WriteLine("echo \"\"");
            }
            finally
            {
                stream.Close();
                FileOperationList.Add(new FileDeleteOperation(configFileFullPath));
            }
        }
    }
}
