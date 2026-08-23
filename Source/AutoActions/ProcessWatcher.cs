using AutoActions.Threading;
using AutoActions.UWP;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoActions
{
    public class ProcessWatcher : IManagedThread
    {
        public bool OneProcessIsRunning { get; private set; } = false;
        public bool OneProcessIsFocused { get; private set; } = false;

        Thread _watchProcessThread = null;

        readonly object _applicationsLock = new object();
        readonly object _accessLock = new object();

        Dictionary<ApplicationItem, ApplicationState> _applications = new Dictionary<ApplicationItem, ApplicationState>();
        public IReadOnlyDictionary<ApplicationItem, ApplicationState> Applications
        {
            get
            {
                lock (_applicationsLock)
                    return new ReadOnlyDictionary<ApplicationItem, ApplicationState>(_applications.ToDictionary(entry => entry.Key, entry => entry.Value));
            }
        }

        bool _stopRequested = false;
        bool _isRunning = false;
        public bool IsRunning { get => _isRunning; private set => _isRunning = value; }

        public bool ManagedThreadIsActive => IsRunning;

        public event EventHandler<string> NewLog;
        //public event EventHandler OneProcessIsRunningChanged;
        //public event EventHandler FocusedProcessChanged;
        public event EventHandler<ApplicationChangedEventArgs> ApplicationChanged;


        public ProcessWatcher()

        {
        }


        private void CallNewLog(string logMessage)
        {
            NewLog?.Invoke(this, logMessage);
        }
  
        public void AddProcess(ApplicationItem application)
        {
            lock (_applicationsLock)
            {
                if (!_applications.ContainsKey(application))
                {
                    _applications.Add(application, ApplicationState.None);
                    CallNewLog($"Application added to process watcher: {application}");
                }
            }
        }

        public void RemoveProcess(ApplicationItem application)
        {
            lock (_applicationsLock)
            {
                if (_applications.ContainsKey(application))
                {
                    _applications.Remove(application);
                    CallNewLog($"Application removed process watcher: {application}");
                }
            }
        }


        public void StartManagedThread()
        {
            if (_stopRequested || IsRunning)
                return;
            lock (_accessLock)
            {
                CallNewLog($"Starting process watcher...");
                //startWatch.Start();
                //stopWatch.Start();
                _isRunning = true;
                _watchProcessThread = new Thread(WatchProcessLoop);
                _watchProcessThread.IsBackground = true;
                _watchProcessThread.Start();
                CallNewLog($"Process watcher started");
            }
        }

        public void StopManagedThread()
        {
            if (_stopRequested || !IsRunning)
                return;
            lock (_accessLock)
            {
                CallNewLog($"Stopping process watcher...");
                _stopRequested = true;
                _watchProcessThread.Join();
                _stopRequested = false;
                _isRunning = false;
                _watchProcessThread = null;
                CallNewLog($"Process watcher stopped.");
            }
        }

        private void WatchProcessLoop()
        {
            while (!_stopRequested)
            {
                // A transient failure (e.g. a process exiting mid-enumeration, or the
                // foreground window disappearing behind the secure desktop) must never
                // escape this loop — an unhandled exception on a background thread
                // silently terminates the entire process.
                try
                {
                    lock (_applicationsLock)
                        UpdateApplications();
                }
                catch (Exception ex)
                {
                    CallNewLog($"Process watcher iteration failed: {ex.Message}");
                }
                Thread.Sleep(Globals.GlobalRefreshInterval);
            }
        }

        private void CallApplicationChanged(ApplicationItem application, ApplicationChangedType changedType)
        {
            ApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(application,changedType));

        }
        private void UpdateApplications()
        {

            lock (_applicationsLock)
            {


                List<ApplicationItem> applications = _applications.Select(a => a.Key).ToList();

                Process[] processes = Process.GetProcesses();
                // Resolve the foreground process once per round and compare PIDs —
                // this avoids re-creating Process objects for every watched entry.
                int foregroundProcessId = GetForegroundProcessId();

                foreach (ApplicationItem application in applications)
                {
                    bool callNewRunning = false;
                    bool callGotFocus = false;
                    bool callLostFocus = false;
                    bool callClosed = false;
                    ApplicationState state = ApplicationState.None;
                    ApplicationState oldState = _applications[application];
                    foreach (var process in processes)
                    {
                        string processName;
                        if (process.ProcessName == "WWAHost")
                        {
                            processName = UWP.WWAHostHandler.GetProcessName(process.Id);
                        }
                        else
                            processName = process.ProcessName;
                        if (application.ApplicationName.ToUpperInvariant().Equals(processName.ToUpperInvariant())
                            || (application.IsUWP && !string.IsNullOrEmpty(application.UWPIdentity) && processName.Contains(application.UWPIdentity)))
                        {

                            state = ApplicationState.Running;

                            if (oldState == ApplicationState.None)
                                callNewRunning = true;
                            if (process.Id == foregroundProcessId)
                            {
                                state = ApplicationState.Focused;
                                if (oldState != ApplicationState.Focused)
                                    callGotFocus = true;
                            }
                            else
                            {
                                if (oldState == ApplicationState.Focused)
                                    callLostFocus = true;

                            }
                        }
                    }
                    if (state == ApplicationState.None && oldState != ApplicationState.None)
                        callClosed = true;

                    _applications[application] = state;
                    if (callNewRunning)
                        CallApplicationChanged(application, ApplicationChangedType.Started);
                    if (callGotFocus)
                        CallApplicationChanged(application, ApplicationChangedType.GotFocus);
                    if (callLostFocus)
                        CallApplicationChanged(application, ApplicationChangedType.LostFocus);
                    if (callClosed)
                        CallApplicationChanged(application, ApplicationChangedType.Closed);
                }

                // GetProcesses() hands out disposable objects; without this the
                // watcher leaked a few hundred handles per round until the GC ran.
                foreach (var process in processes)
                    process.Dispose();
            }
        }

        /// <summary>
        /// Returns the process ID of the foreground application, resolving UWP apps
        /// hosted by ApplicationFrameHost to their real child process. Returns -1
        /// when there is no usable foreground window (secure desktop on lock
        /// screen/UAC, transient states while windows switch, or the process dying
        /// mid-lookup) — callers treat -1 as "nothing focused".
        /// </summary>
        private int GetForegroundProcessId()
        {
            try
            {
                IntPtr foregroundWindow = WinAPIFunctions.GetforegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return -1;

                int processId = WinAPIFunctions.GetWindowProcessId(foregroundWindow);
                if (GetProcessNameById(processId) != "ApplicationFrameHost")
                    return processId;

                int realProcessId = -1;
                WinAPIFunctions.WindowEnumProc callback = (hwnd, lparam) =>
                {
                    int childProcessId = WinAPIFunctions.GetWindowProcessId(hwnd);
                    if (childProcessId > 0 && GetProcessNameById(childProcessId) != "ApplicationFrameHost")
                    {
                        realProcessId = childProcessId;
                        return false; // first non-host child is the real UWP process
                    }
                    return true;
                };
                using (var hostProcess = Process.GetProcessById(processId))
                {
                    WinAPIFunctions.EnumChildWindows(hostProcess.MainWindowHandle, callback, IntPtr.Zero);
                }
                return realProcessId != -1 ? realProcessId : processId;
            }
            catch (Exception)
            {
                // The foreground process can exit between window lookup and here;
                // report "nothing focused" for this round instead of crashing.
                return -1;
            }
        }

        private static string GetProcessNameById(int processId)
        {
            if (processId <= 0)
                return string.Empty;
            try
            {
                using (var process = Process.GetProcessById(processId))
                    return process.ProcessName;
            }
            catch (ArgumentException)
            {
                // Process already exited.
                return string.Empty;
            }
        }


    }
}
