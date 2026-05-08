using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ULMSWinFormsApp.Forms;

namespace ULMSWinFormsApp
{
    public partial class FrmLogin : Form
    {
        //  Brute-force lockout state 
        private static readonly Dictionary<string, int> _failedAttempts = new();
        private static readonly Dictionary<string, DateTime> _lockoutUntil = new();
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 15;

        //  Simulated user store (replace with DB calls in production) 
        //    Passwords stored as hashed strings; here we compare plaintext
        //    only for demonstration – swap GetUser() for a real lookup.
        private static readonly Dictionary<string, (string Password, string Role)> _users = new()
        {
            { "student01", ("Pass@123",   "student") },
            { "admin",     ("Admin@2025", "admin")   },
        };

        public FrmLogin()
        {
            InitializeComponent();
        }

        //  TC-LV-01 / TC-LV-09  Valid credentials 
        //  TC-LV-02 / TC-LV-03  Wrong or unknown credentials 
        //  TC-LV-04 / TC-LV-05 / TC-LV-06  Empty-field guards 
        //  TC-LV-07  SQL-injection safe (no DB string concat) 
        //  TC-LV-08  Brute-force lockout 
        //  TC-LV-10  Case-sensitive password comparison 
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;          // do NOT trim passwords

            //  TC-LV-04 / TC-LV-05 / TC-LV-06  Empty-field validation 
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username is required.\nPassword is required.",
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username is required.",
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required.",
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            //  TC-LV-08  Check lockout before any credential lookup 
            if (_lockoutUntil.TryGetValue(username, out DateTime lockedUntil)
                && DateTime.UtcNow < lockedUntil)
            {
                int minutesLeft = (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes);
                MessageBox.Show($"Too many attempts. Try again in {minutesLeft} minute(s).",
                                "Account Locked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //  TC-LV-01 / TC-LV-02 / TC-LV-03 / TC-LV-09 / TC-LV-10 
            // FIX: was (username == "admin" || password == "1234")
            //      – OR let anyone in with just one matching field.
            //      Now we use AND, case-sensitive, with a proper user lookup.
            if (_users.TryGetValue(username, out var record)
                && string.Equals(record.Password, password, StringComparison.Ordinal)) // TC-LV-10
            {
                // Successful login – reset lockout counter
                _failedAttempts.Remove(username);
                _lockoutUntil.Remove(username);

                MessageBox.Show("Login Successful!", "Welcome",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                //  TC-LV-09  Route by role, not a hardcoded username check 
                if (record.Role == "admin")
                {
                    FrmDashboard adminDash = new FrmDashboard();
                    adminDash.Show();
                }
                else
                {
                    FrmDashboard dashboard = new FrmDashboard();
                    dashboard.Show();
                }

                this.Hide();
            }
            else
            {
                // TC-LV-02 / TC-LV-03 / TC-LV-07 / TC-LV-10
                // Generic message – never reveal whether the username exists
                RecordFailedAttempt(username);

                int attempts = _failedAttempts.GetValueOrDefault(username, 0);
                int remaining = MaxAttempts - attempts;

                if (remaining > 0)
                {
                    MessageBox.Show($"Invalid username or password.\n({remaining} attempt(s) remaining.)",
                                    "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // If remaining == 0 the lockout message was already shown inside RecordFailedAttempt
            }
        }

        //  Helper: track failures and trigger lockout 
        private static void RecordFailedAttempt(string username)
        {
            _failedAttempts.TryGetValue(username, out int count);
            count++;
            _failedAttempts[username] = count;

            if (count >= MaxAttempts)
            {
                _lockoutUntil[username] = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                MessageBox.Show($"Too many failed attempts. Account locked for {LockoutMinutes} minutes.",
                                "Account Locked", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //  Clear button 
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtUsername.Clear();
            txtPassword.Clear();
            txtUsername.Focus();
        }
    }
}