using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using blu.FileIO;
using System.Threading.Tasks;
using System.Threading;

namespace blu
{
    public class LoggingModule : Module
    {
        public const bool ENABLE                = true;

        public const bool ENABLE_LOG            = true;
        public const bool ENABLE_WARNING        = true;
        public const bool ENABLE_ERROR          = true;
        public const bool ENABLE_ASSERT         = true;
        public const bool ENABLE_EXCEPTION      = true;

        public const bool ENABLE_LOG_COUNT      = false;
        public const bool ENABLE_STACK_TRACE    = false;
        public const bool ENABLE_DATETIME       = true;

        private const string m_filename = "/log.txt";
        private string m_filepath = null;
        private int m_logCount = 0;
        private Mutex mutex = new Mutex();

        public override void Initialize()
        {
            m_filepath = Application.persistentDataPath + m_filename;
            System.IO.File.Delete(m_filepath);
            Debug.Log("[LoggingModule]: Initializing Logging module");
        }

        private void OnEnable()
        {
            if (ENABLE)
                Application.logMessageReceivedThreaded += Callback;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= Callback;
        }

        private void Callback(string logString, string stackTrace, LogType type)
        {
            string message = "";

#pragma warning disable CS0162 // Unreachable code detected
            switch (type)
            {
                case LogType.Log:
                    if (!ENABLE_LOG) return;
                    message = "[LOG] ";
                    break;

                case LogType.Warning:
                    if (!ENABLE_WARNING) return;
                    message = "[WARNING] ";
                    break;

                case LogType.Error:
                    if (!ENABLE_ERROR) return;
                    message = "[ERROR] ";
                    break;

                case LogType.Exception:
                    if (!ENABLE_EXCEPTION) return;
                    message = "[EXCEPTION] ";
                    break;

                case LogType.Assert:
                    if (!ENABLE_ASSERT) return;
                    message = "[ASSERT] ";
                    break;

                default:
                    return;
            }

            if (ENABLE_LOG_COUNT)
            {
                message += $"[{m_logCount++}] ";
            }

            if (ENABLE_DATETIME)
            {
                message += $"[{DateTime.UtcNow}] ";
            }

            message += logString;
            message += "\n\n";

            if (ENABLE_STACK_TRACE)
            {
                message += stackTrace;
                message += "\n\n\n";
            }

#pragma warning restore CS0162 // Unreachable code detected

            Task.Run(() =>
            WriteToFile(message)
            );
        }

        private void WriteToFile(string message)
        {
            mutex.WaitOne();
            System.IO.File.AppendAllText(m_filepath, message);
            mutex.ReleaseMutex();
        }
    }
}