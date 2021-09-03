using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Forms;

namespace WpfImpersonate
{
    /// <summary>
    /// Project "Impersonate" using WinForms -> found on codeproject.com
    /// https://www.codeproject.com/Articles/4051/Windows-Impersonation-using-C
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowsImpersonationContext newUser;
        private List<string> errors;
        private List<string> output;

        public MainWindow()
        {
            InitializeComponent();

            TxtRunningAs.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            // populate logon domain name
            string sTempUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (sTempUser.IndexOf("\\") != -1)
            {
                string[] aryUser = new String[2];
                char[] splitter = { '\\' };
                aryUser = sTempUser.Split(splitter);
                TxtDomain.Text = aryUser[0];
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // revert to previous user
            if (newUser != null)
            {
                newUser.Undo();
                newUser = null;
            }

            TxtOutput.Text = null;
            // update the running as name
            TxtRunningAs.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }
        private void BtnLogon_Click(object sender, RoutedEventArgs e)
        {
            TxtOutput.Text = null;

            if (TxtUsername.Text != "" && TxtPassword.Password != "")
            {
                try
                {
                    // attempt to impersonate specified user
                    newUser = Impersonation.ImpersonateUser(TxtUsername.Text, TxtPassword.Password, TxtDomain.Text);
                    // update the running as name
                    TxtRunningAs.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                }
                catch (Exception ex)
                {
                    // why did it fail?
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                System.Windows.Forms.MessageBox.Show("Complete all the logon credentials", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (errors == null) errors = new List<string>(); else errors.Clear();
                if (output == null) output = new List<string>(); else output.Clear();

                TxtOutput.Text = null;

                if (StartProcess())
                {
                    TxtOutput.AppendText($"Running at: {WindowsIdentity.GetCurrent().Name}{Environment.NewLine}{Environment.NewLine}");

                    if (errors.Count > 0)
                    {
                        TxtOutput.AppendText($"!ERROR (starting 'run.bat'):{Environment.NewLine}");
                        TxtOutput.AppendText($"-------------------------------------------------------------------------------{ Environment.NewLine}");


                        var errorsTxt = String.Join(Environment.NewLine, errors.ToArray());
                        TxtOutput.AppendText(errorsTxt + Environment.NewLine);
                        TxtOutput.AppendText($"-------------------------------------------------------------------------------{ Environment.NewLine}");
                    }
                    else
                    {
                        TxtOutput.AppendText($"OUTPUT:{Environment.NewLine}");
                        var outputTxt = String.Join(Environment.NewLine, output.ToArray());
                        TxtOutput.AppendText(outputTxt);
                    }

                }

            }
            catch (Exception ex)
            {
                // why did it fail?
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool StartProcess()
        {
            // Create a Process instance
            System.Diagnostics.Process proc = new System.Diagnostics.Process();

            // Setup for starting 'run.bat'
            proc.StartInfo.FileName = @"run.bat";
            // Alternatively call 'whoami' or any other executable directly via Startinfo
            // proc.StartInfo.FileName = "whoami";

            if (RbUseCredentials.IsChecked.Value)
            {
                if (string.IsNullOrWhiteSpace(TxtUsername.Text)
                    || string.IsNullOrWhiteSpace(TxtPassword.Password)
                    || string.IsNullOrWhiteSpace(TxtDomain.Text))
                {
                    System.Windows.Forms.MessageBox.Show("Complete all the logon credentials", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
                else
                {
                    // Set credentials
                    proc.StartInfo.Domain = TxtDomain.Text;
                    proc.StartInfo.UserName = TxtUsername.Text;
                    System.Security.SecureString ssPwd = new System.Security.SecureString();
                    for (int x = 0; x < TxtPassword.Password.Length; x++)
                    {
                        ssPwd.AppendChar(TxtPassword.Password[x]);
                    }
                    proc.StartInfo.Password = ssPwd;
                }
            }

            // StartInfo setup
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(OutputErrorHandler);

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return true;
        }

        private void OutputErrorHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                errors.Add(e.Data);
        }
        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            output.Add(e.Data);
        }

    }
}
