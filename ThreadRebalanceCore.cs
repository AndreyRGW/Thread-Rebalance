using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SetRandomIdealProcessor
{
    class Program
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

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ThreadRebalance <PID or Process Name> <interval>");
                return;
            }

            int pid;
            if (!int.TryParse(args[0], out pid))
            {
                Process[] processes = Process.GetProcessesByName(args[0]);
                if (processes.Length == 0)
                {
                    Console.WriteLine("Process not found");
                    return;
                }
                pid = processes[0].Id;
            }
            int interval = int.Parse(args[1]);

            Process process = Process.GetProcessById(pid);
            Random random = new Random();

            while (true)
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
