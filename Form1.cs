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
            button1.Enabled = false; // Изначально кнопка "Rebalance" неактивна
        }

        private void InitializeProcessCheckTimer()
        {
            processCheckTimer = new System.Windows.Forms.Timer();
            processCheckTimer.Interval = 1000; // Проверка каждую секунду
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
            var processes = Process.GetProcesses()
                                   .OrderBy(p => p.ProcessName)
                                   .Select(p => $"{p.ProcessName} (ID: {p.Id})")
                                   .ToList();
            listBox1.Items.AddRange(processes.ToArray());
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
                button1.Enabled = true; // Активируем кнопку "Rebalance" после выбора процесса
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
    }
}
