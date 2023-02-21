using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ThreadRebalanceGUI
{
    internal class RebalanceCore
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern bool SetThreadIdealProcessor(IntPtr hThread, int dwIdealProcessor);

        [DllImport("kernel32.dll")]
        static extern bool SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

        [DllImport("kernel32.dll")]
        static extern bool GetThreadIdealProcessorEx(IntPtr hThread, ref PROCESSOR_NUMBER lpIdealProcessor);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSOR_NUMBER
        {
            public ushort Group;
            public byte Number;
            public byte Reserved;
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200),
            THREAD_ALL_ACCESS = (0x1F03FF)
        }

        public enum RebalanceStatus
        {
            Idle,
            Running,
            Cancelled
        }

        public static volatile RebalanceStatus Status = RebalanceStatus.Idle;

        private static bool continueRebalancing = true;

        public static void StopRebalancing()
        {
            continueRebalancing = false;
        }

        public static void StartRebalancing()
        {
            continueRebalancing = true;
        }

        public void Core(int pid, int interval)
        {
            Process process = Process.GetProcessById(pid);
            Random random = new Random();

            while (continueRebalancing)
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    IntPtr hThread = OpenThread(ThreadAccess.THREAD_ALL_ACCESS, false, (uint)thread.Id);
                    PROCESSOR_NUMBER processorNumber = new PROCESSOR_NUMBER();
                    if (GetThreadIdealProcessorEx(hThread, ref processorNumber))
                    {
                        Console.WriteLine("Thread {0} ideal processor is {1}", thread.Id, processorNumber.Number);

                        // Set a random ideal processor, unless the current one is 11 (max value)
                        int idealProcessor = processorNumber.Number;
                        {
                            int newIdealProcessor;
                            do
                            {
                                newIdealProcessor = random.Next(0, Environment.ProcessorCount);
                            } while (newIdealProcessor == idealProcessor);

                            Console.WriteLine("Setting thread {0} ideal processor to {1}", thread.Id, newIdealProcessor);
                            SetThreadIdealProcessor(hThread, newIdealProcessor);
                            idealProcessor = newIdealProcessor;
                        }

                        // Set the affinity mask to the ideal processor and the next available processor
                        int nextProcessor = idealProcessor < Environment.ProcessorCount - 1 ? idealProcessor + 1 : random.Next(Environment.ProcessorCount);
                        IntPtr affinityMask = (IntPtr)((1 << idealProcessor) | (1 << nextProcessor));
                        SetThreadAffinityMask(hThread, affinityMask);
                    }
                    else
                    {
                        Console.WriteLine("Thread {0} ideal processor is unknown", thread.Id);
                    }
                    CloseHandle(hThread);
                }
                System.Threading.Thread.Sleep(interval * 1000);
            }
        }
    }
}
