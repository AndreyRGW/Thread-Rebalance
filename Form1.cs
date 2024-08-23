using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;


namespace ThreadRebalanceGUI
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer processCheckTimer;
        private int selectedProcessId = -1;
        private Process selectedProcessInstance;

        public Form1()
        {
            InitializeComponent();
            DisplayProcess();
            InitializeProcessCheckTimer();
            button1.Enabled = false; // Initially, the "Rebalance" button is inactive
        }

        private void InitializeProcessCheckTimer()
        {
            processCheckTimer = new System.Windows.Forms.Timer();
            processCheckTimer.Interval = 1000; // Check every second
            processCheckTimer.Tick += ProcessCheckTimer_Tick;
            processCheckTimer.Start();
        }

        private void ProcessCheckTimer_Tick(object sender, EventArgs e)
        {
            if (selectedProcessInstance != null && selectedProcessInstance.HasExited)
            {
                label3.Text = "none";
                button1.Enabled = false;
                button2.Enabled = false;
                label5.Text = "not running";
                selectedProcessInstance = null;
                selectedProcessId = -1;
            }
        }

        private void DisplayProcess()
        {
            listBox1.Items.Clear(); // Clear listBox1 before adding new processes

            var processes = Process.GetProcesses()
                                   .OrderBy(p => p.ProcessName)
                                   .ToList();

            listBox1.Items.AddRange(processes.Select(p => $"{p.ProcessName} (ID: {p.Id})").ToArray());

            int totalProcesses = processes.Count;
            int totalThreads = processes.Sum(p => p.Threads.Count);

            label9.Text = $"{totalProcesses} (threads {totalThreads})"; // Update the label with total processes and threads
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                string selectedProcess = listBox1.SelectedItem.ToString();
                int startIndex = selectedProcess.IndexOf("(ID: ") + 5;
                int endIndex = selectedProcess.IndexOf(")", startIndex);
                string selectedProcessIdString = selectedProcess.Substring(startIndex, endIndex - startIndex);
                selectedProcessId = int.Parse(selectedProcessIdString);

                string selectedProcessName = selectedProcess.Substring(0, startIndex - 6).Trim();
                label3.Text = $"{selectedProcessName} (ID: {selectedProcessId})";

                selectedProcessInstance = Process.GetProcessById(selectedProcessId);
                button1.Enabled = true; // Activate the "Rebalance" button after selecting a process
            }
            else
            {
                MessageBox.Show("Please select a process from the list first.");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (selectedProcessId < 0)
            {
                MessageBox.Show("Please select a process to rebalance.");
                return;
            }

            if (!int.TryParse(textBox1.Text, out int interval) || interval <= 0)
            {
                MessageBox.Show("Please enter a valid interval value.");
                return;
            }

            label5.Text = "running";
            button1.Enabled = false;
            button2.Enabled = true;

            RebalanceCore rebalanceCore = new RebalanceCore();
            await Task.Run(() => rebalanceCore.Core(selectedProcessId, interval));

            RebalanceCore.StartRebalancing();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DisplayProcess();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            RebalanceCore.StopRebalancing();
            label5.Text = "not running";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            DisplayProcess();
        }

        private void label7_Click(object sender, EventArgs e)
        {
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false; // Disable the button to prevent multiple clicks

            int totalProcesses = 0;
            int totalThreads = 0;
            int successfulRebalances = 0;
            int failedRebalances = 0;

            foreach (var item in listBox1.Items)
            {
                string processInfo = item.ToString();
                int startIndex = processInfo.IndexOf("(ID: ") + 5;
                int endIndex = processInfo.IndexOf(")", startIndex);
                string processIdString = processInfo.Substring(startIndex, endIndex - startIndex);
                int processId = int.Parse(processIdString);

                try
                {
                    Process process = Process.GetProcessById(processId);
                    totalProcesses++;
                    totalThreads += process.Threads.Count;

                    RebalanceCoreSingle rebalanceCoreSingle = new RebalanceCoreSingle();
                    await Task.Run(() => rebalanceCoreSingle.Core(processId)); // Run the rebalancing task asynchronously
                    successfulRebalances++;
                }
                catch (Exception ex)
                {
                    // Displaying an error message for debugging
                    Console.WriteLine($"Error processing process with ID {processId}: {ex.Message}");
                    failedRebalances++;
                }
            }

            label9.Text = $"{totalProcesses} (threads {totalThreads})"; // Update the label with total processes and threads
            label10.Text = $"{successfulRebalances} (with errors: {failedRebalances})"; // Update the label with successful and failed rebalances

            button5.Enabled = true; // Re-enable the button
        }
    }
}
