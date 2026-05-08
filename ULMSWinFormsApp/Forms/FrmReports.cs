using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ULMSWinFormsApp.Models;

namespace ULMSWinFormsApp.Forms
{
    public partial class FrmReports : Form
    {
        //  Simulated logged-in student (set this on login — TC-RG-07) 
        public static string LoggedInStudentId { get; set; } = "S001";
        public static bool IsAdmin { get; set; } = false;

        //  Simulated student data store 
        // Key: StudentId | Value: StudentRecord
        private static readonly Dictionary<string, StudentRecord> _students = new()
        {
            {
                "S001", new StudentRecord
                {
                    StudentId   = "S001",
                    StudentName = "John Dube",
                    Programme   = "Software Engineering",
                    Year        = 2025,
                    Status      = "Active",
                    // TC-RG-01: all 5 subjects captured
                    Marks = new Dictionary<string, double?>
                    {
                        { "Programming 1",   70 },
                        { "Web Development", 80 },
                        { "Software Testing",60 },
                        { "Database System", 90 },
                        { "Mathematics",     50 },
                    }
                }
            },
            {
                "S002", new StudentRecord
                {
                    StudentId   = "S002",
                    StudentName = "Jane Smith",
                    Programme   = "Information Technology",
                    Year        = 2025,
                    Status      = "Active",
                    // TC-RG-04: only 3 of 5 subjects have marks
                    Marks = new Dictionary<string, double?>
                    {
                        { "Programming 1",   65  },
                        { "Web Development", 72  },
                        { "Software Testing", null },   // missing
                        { "Database System",  null },   // missing
                        { "Mathematics",     58  },
                    }
                }
            },
            {
                "S005", new StudentRecord
                {
                    StudentId   = "S005",
                    StudentName = "Sipho Nkosi",
                    Programme   = "Computer Science",
                    Year        = 2025,
                    Status      = "Active",
                    // TC-RG-10: enrolled but no marks at all
                    Marks = new Dictionary<string, double?>
                    {
                        { "Programming 1",    null },
                        { "Web Development",  null },
                        { "Software Testing", null },
                        { "Database System",  null },
                        { "Mathematics",      null },
                    }
                }
            },
        };

        //  Last generated report text (used for export — TC-RG-08) 
        private string _lastReportText = string.Empty;

        public FrmReports()
        {
            InitializeComponent();
        }

        //  TC-RG-05 / TC-RG-06  Async generation — no UI freeze 
        private async void btnGenerateReport_Click(object sender, EventArgs e)
        {
            string reportType = cmbReportType.Text.Trim();
            string studentId = txtReportStudentId.Text.Trim();

            //  Basic input guards 
            if (string.IsNullOrWhiteSpace(reportType))
            {
                ShowError("Please select a report type.");
                cmbReportType.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(studentId))
            {
                ShowError("Student ID is required.");
                txtReportStudentId.Focus();
                return;
            }

            //  TC-RG-07  Access control 
            // Non-admin students may only view their own report.
            if (!IsAdmin &&
                !string.Equals(LoggedInStudentId, studentId, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("You do not have permission to view this report.");
                return;
            }

            //  TC-RG-01 / TC-RG-02 / TC-RG-04 / TC-RG-10  Student lookup 
            if (!_students.TryGetValue(studentId, out StudentRecord student))
            {
                ShowError($"No record found for Student ID '{studentId}'.");
                return;
            }

            // Disable button and show loading state
            btnGenerateReport.Enabled = false;
            txtReportOutput.Text = "Generating report, please wait...";

            //  TC-RG-05  Run generation off the UI thread (target < 3 s)
            string reportText = await Task.Run(() => BuildReport(reportType, student));

            _lastReportText = reportText;
            txtReportOutput.Text = reportText;
            btnGenerateReport.Enabled = true;
        }

        //  Core report builder 
        private static string BuildReport(string reportType, StudentRecord student)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("===== ULMS REPORT =====");
            sb.AppendLine($"Report Type:  {reportType}");
            sb.AppendLine($"Student ID:   {student.StudentId}");    // TC-RG-02
            sb.AppendLine($"Student Name: {student.StudentName}");  // TC-RG-02
            sb.AppendLine($"Year:         {student.Year}");         // TC-RG-02
            sb.AppendLine($"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            if (reportType == "Student Summary Report")
            {
                sb.AppendLine($"Programme: {student.Programme}");
                sb.AppendLine($"Status:    {student.Status}");
            }
            else if (reportType == "Marks Report")
            {
                AppendMarksSection(sb, student);
            }
            else if (reportType == "Enrollment Report")
            {
                int idx = 1;
                foreach (string subject in student.Marks.Keys)
                    sb.AppendLine($"Course {idx++}: {subject}");
                sb.AppendLine("Semester: Semester 1");
            }
            else
            {
                sb.AppendLine("Unknown report type selected.");
            }

            return sb.ToString();
        }

        //  Marks section 
        // TC-RG-01 / TC-RG-03 / TC-RG-04 / TC-RG-09 / TC-RG-10
        private static void AppendMarksSection(StringBuilder sb, StudentRecord student)
        {
            var captured = new List<double>();
            var missing = new List<string>();

            foreach (var kvp in student.Marks)
            {
                if (kvp.Value.HasValue)
                {
                    sb.AppendLine($"  {kvp.Key,-22}: {kvp.Value.Value:F2}");
                    captured.Add(kvp.Value.Value);
                }
                else
                {
                    // TC-RG-04: clearly flag missing subjects
                    sb.AppendLine($"  {kvp.Key,-22}: ** MARK NOT CAPTURED **");
                    missing.Add(kvp.Key);
                }
            }

            sb.AppendLine();

            if (captured.Count == 0)
            {
                // TC-RG-10: no marks at all — no crash, meaningful message
                sb.AppendLine("Average:      N/A (no marks captured)");
                sb.AppendLine("Final Result: N/A");
            }
            else
            {
                // TC-RG-03: correct average — (sum of captured marks) / count
                // FIX: original was Subject1 + Subject2 + Subject3/3 (precedence bug)
                //      and showed "Average: 169" instead of the correct value.
                double average = captured.Sum() / captured.Count;
                sb.AppendLine($"Average:      {average:F2}");           // TC-RG-03
                sb.AppendLine($"Final Result: {(average >= 50 ? "PASS" : "FAIL")}");

                if (missing.Count > 0)
                {
                    // TC-RG-04: note partial average
                    sb.AppendLine();
                    sb.AppendLine($"NOTE: Average calculated from {captured.Count} captured " +
                                  $"subject(s) only. {missing.Count} subject(s) missing.");
                }
            }
        }

        //  TC-RG-08  Export as PDF 
        // Writes plain-text .txt as a stand-in; swap in a PDF library
        // (e.g. iTextSharp / QuestPDF) for a real PDF in production.
        private void btnExportPdf_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastReportText))
            {
                ShowError("Please generate a report before exporting.");
                return;
            }

            using SaveFileDialog dlg = new SaveFileDialog
            {
                Title = "Export Report",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"ULMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExt = "txt",
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, _lastReportText, Encoding.UTF8);
                MessageBox.Show($"Report exported successfully to:\n{dlg.FileName}",
                                "Export Complete", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }

        //  TC-RG-09  Reload / refresh report after mark edits 
        // Clicking Generate again re-reads _students (which FrmMarksCapture updates)
        // so the latest marks are always reflected — no stale data.
        // This method can be called externally by FrmMarksCapture after saving:
        //   FrmReports.UpdateStudentMark("S001", "Programming 1", 90);
        public static void UpdateStudentMark(string studentId, string subject, double newMark)
        {
            if (_students.TryGetValue(studentId, out StudentRecord s) &&
                s.Marks.ContainsKey(subject))
            {
                s.Marks[subject] = newMark;   // TC-RG-09: in-memory update, live on next Generate
            }
        }

        //  Clear button 
        private void btnClearReport_Click(object sender, EventArgs e)
        {
            cmbReportType.SelectedIndex = -1;
            txtReportStudentId.Clear();
            txtReportOutput.Clear();
            _lastReportText = string.Empty;
            txtReportStudentId.Focus();
        }

        //  Back button 
        private void btnBackReport_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

   
    public class StudentRecord : StudentRecord1
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string Programme { get; set; }
        public int Year { get; set; }
        public string Status { get; set; }

        // Nullable double so missing marks are distinguished from zero (TC-RG-04/10)
        public Dictionary<string, double?> Marks { get; set; } = new();
    }
}