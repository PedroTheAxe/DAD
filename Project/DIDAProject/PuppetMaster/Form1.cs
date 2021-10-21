﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int size = -1;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                try
                {
                    string text = File.ReadAllText(file);
                    size = text.Length;
                    Console.WriteLine(text); // only to debug
                    string[] commands = text.Split('\n');
                    
                    foreach(string s in commands) {
                        commandParser(s);
                    }
                }
                catch (IOException)
                {
                }
            }
            // for debugging use.
            Console.WriteLine(size);
            Console.WriteLine(result); 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            commandParser(textBox1.Text);
            //MessageBox.Show(textBox1.Text + "\r\n" + "Command executed.");
        }

        private void commandParser(string command)
        {
            string[] instance = command.Split(' ');
            string arguments;
            string fileName;
            switch (instance[0])
            {
                case "scheduler":
                    Console.WriteLine("entered scheduler\r\n");
                    arguments = instance[1];
                    fileName = "DIDASchedulerUI";
                    processCreationService(fileName, arguments);
                    break;

                case "storage":
                    Console.WriteLine("entered storage\r\n");
                    arguments = instance[1] + instance[2];
                    fileName = "DIDAStorageUI";
                    processCreationService(fileName, arguments);
                    break;

                case "worker":
                    Console.WriteLine("entered worker\r\n");
                    arguments = instance[1] + instance[2];
                    fileName = "DIDAWorkerUI";
                    processCreationService(fileName, arguments);
                    break;

                case "populate":
                    Console.WriteLine("entered populate\r\n");
                    //arguments = instance[1];
                    //fileName = "";
                    //processCreationService(fileName, arguments);
                    break;

                case "client":
                    Console.WriteLine("entered client\r\n");
                    //arguments = instance[1];
                    //fileName = "";
                    //processCreationService(fileName, arguments);
                    break;
            }
        }

        private static void processCreationService(string fileName, string args)
        {
            try
            {
                string execName = fileName + ".exe";
                string directory = System.IO.Directory.GetParent(Environment.CurrentDirectory).ToString();
                string applicationPath = Path.GetFullPath(Path.Combine(directory, @"..\..\", fileName, @"bin\Debug\netcoreapp3.1\", execName));
                Console.WriteLine(applicationPath);

                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = applicationPath;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.Arguments = args;
                    process.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
