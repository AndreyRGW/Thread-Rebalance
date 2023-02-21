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
        private void DisplayProcess()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                listBox1.Items.Add(p.ProcessName + " (ID: " + p.Id + ")");
            }

        }

        private int selectedProcessId = -1;


        public Form1()
        {
            InitializeComponent();
            DisplayProcess();
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

                startIndex = 0;
                endIndex = selectedProcess.IndexOf("(ID: ");
                string selectedProcessName = selectedProcess.Substring(startIndex, endIndex - startIndex);

                label3.Text = "" + selectedProcessName.Trim() + " (ID: " + selectedProcessId + ")";
            }
            else
            {
                MessageBox.Show("Please select a process from the list first.");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int pid = selectedProcessId;

            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Please enter a valid interval value.");
                return;
            }

            if (selectedProcessId < 0)
            {
                MessageBox.Show("Please select a process to rebalance.");
                return;
            }

            int interval;
            if (!int.TryParse(textBox1.Text, out interval))
            {
                MessageBox.Show("Please enter a valid interval value.");
                return;
            }

            // Run the Core() method in a separate thread
            RebalanceCore rebalanceCore = new RebalanceCore();
            await Task.Run(() => rebalanceCore.Core(pid, interval));

            // Enable the rebalance button and disable the cancel button
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
            RebalanceCore.StopRebalancing();
        }
    }
}
