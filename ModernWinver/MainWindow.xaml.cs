﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Management;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace ModernWinver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {

            InitializeComponent();
            DateTime current = DateTime.Now;
            string ValuesPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            bool UpToDate = true;
            string JsonList = "";
            Values vals = new Values();

            if (!File.Exists(System.IO.Path.Combine(ValuesPath, "winver.json")))
            {
                File.Create(System.IO.Path.Combine(ValuesPath, "winver.json")).Close();
                UpToDate = false;
            }
            else
            {
                JsonList = File.ReadAllText(System.IO.Path.Combine(ValuesPath, "winver.json"));
                if (JsonList == "")
                {
                    UpToDate = false;
                }
                else
                {
                    vals = JsonConvert.DeserializeObject<Values>(JsonList);
                    if (vals.WeekOfYear != current.DayOfYear / 7  || vals.WeekOfYear != -1)
                    {
                        UpToDate = false;
                    }
                }
            }

            if (UpToDate == false)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                vals.WeekOfYear = current.DayOfYear / 7;
                vals.CopyrightYear = current.Year.ToString();
                
                // Get edition of Windows 10 because apparently that's bloody impossible any other way and the registry returns me wrong values
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");

                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        vals.Edition = ((string)queryObj["Caption"]).Replace("Microsoft ", "");
                    }
                }
                catch (ManagementException e)
                {
                    MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
                }
                
                // These three are simple, just getting from the registry like a normal person
                vals.Version = (string)key.GetValue("ReleaseId");
                vals.Build = (string)key.GetValue("CurrentBuild") + "." + key.GetValue("UBR").ToString();
                vals.User = (string)key.GetValue("RegisteredOwner");
                
                // This just prevents you from having a blank username
                if (vals.User == "" || vals.User == "user name")
                {
                    vals.User = "(Unknown user)";
                }

                // If in org, show org name, else show hostname
                if ((string)key.GetValue("RegisteredOrganization") == "")
                {
                    vals.IsLocal = true;
                    vals.Workgroup = ExecuteCommandSync("hostname").Replace("\r\n", "");
                }
                else
                {
                    vals.IsLocal = false;
                    vals.Workgroup = (string)key.GetValue("RegisteredOrganization");
                }
                File.Create(System.IO.Path.Combine(ValuesPath, "winver.json")).Close();
                File.WriteAllText(System.IO.Path.Combine(ValuesPath, "winver.json"), JsonConvert.SerializeObject(vals, Formatting.Indented));


            }

            // Switches over the label from Workgroup to Computer if local
            if (vals.IsLocal)
            {
                labelWorkgroup.Content = "Computer";
            }


            // Actually sets all the labels
            valueCopyright.Content = "© " + vals.CopyrightYear + " Microsoft Corporation. All rights reserved.";
            valueEdition.Content = vals.Edition;
            valueVersion.Content = vals.Version;
            valueBuild.Content = vals.Build;
            valueUser.Content = vals.User;
            valueWorkgroup.Content = vals.Workgroup;

            // Shows the window
            Show();
        }

        struct Values
        {
            public int WeekOfYear;
            public string CopyrightYear;
            public string Edition;
            public string Version;
            public string Build;
            public string User;
            public bool IsLocal;
            public string Workgroup;
        }

        public string ExecuteCommandSync(object command)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                ProcessStartInfo procStartInfo =
                    new ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                return result;
            }
            catch (Exception e)
            {
                // Log the exception
                return e.ToString();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonLaunchSettings_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("ms-settings:about");
            Close();
        }
    }
}
