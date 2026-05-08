using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ULMSWinFormsApp.Models;

namespace ULMSWinFormsApp.Forms
{
    public partial class FrmMarksCapture : Form
    {
        //  Saved marks store 
        // Key: "StudentId|SubjectLabel" | Value: MarkRecord
        // Static so records persist across form instances (TC-MC-10 edit/overwrite)
        private static readonly Dictionary<string, MarkRecord> _savedMarks = new();

        //  Audit trail (TC-MC-10) 
        private static readonly List<string> _auditTrail = new();

        public FrmMarksCapture()
        {
            InitializeComponent();
        }

        private void btnCalculateResults_Click(object sender, EventArgs e)
        {
            string studentId = txtMarkStudentId.Text.Trim();
            string studentName = txtMarkStudentName.Text.Trim();

            //  Field presence checks 
            if (string.IsNullOrWhiteSpace(studentId))
            {
                ShowError("Student ID is required.");
                txtMarkStudentId.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(studentName))
            {
                ShowError("Student Name is required.");
                txtMarkStudentName.Focus();
                return;
            }

            //  TC-MC-08  Empty marks fields 
            if (string.IsNullOrWhiteSpace(txtSubject1.Text) ||
                string.IsNullOrWhiteSpace(txtSubject2.Text) ||
                string.IsNullOrWhiteSpace(txtSubject3.Text))
            {
                ShowError("Mark is required. Please fill in all three subject marks.");
                return;
            }

            //  TC-MC-06 / TC-MC-09  Non-numeric / special character input 
            //  TC-MC-07  Decimal handling 
            if (!TryParseAndValidateMark(txtSubject1.Text, "Subject 1", out double s1)) return;
            if (!TryParseAndValidateMark(txtSubject2.Text, "Subject 2", out double s2)) return;
            if (!TryParseAndValidateMark(txtSubject3.Text, "Subject 3", out double s3)) return;

            //  TC-MC-04 / TC-MC-05  Range 0–100 already checked inside helper 
            double average = (s1 + s2 + s3) / 3.0;
            average = Math.Round(average, 2);

            string resultStatus = average >= 50 ? "PASS" : "FAIL";

            MarkRecord record = new MarkRecord
            {
                StudentId = studentId,
                StudentName = studentName,
                Subject1 = s1,
                Subject2 = s2,
                Subject3 = s3,
                Average = average,
                ResultStatus = resultStatus
            };

            //  TC-MC-10  Edit existing — overwrite with audit trail 
            string recordKey = $"{studentId}|{studentName}";
            bool isUpdate = _savedMarks.ContainsKey(recordKey);

            if (isUpdate)
            {
                MarkRecord old = _savedMarks[recordKey];
                _auditTrail.Add(
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UPDATED {studentId} | " +
                    $"S1: {old.Subject1}→{s1}  S2: {old.Subject2}→{s2}  S3: {old.Subject3}→{s3}  " +
                    $"Avg: {old.Average}→{average}  Result: {old.ResultStatus}→{resultStatus}");
            }
            else
            {
                _auditTrail.Add(
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SAVED {studentId} | " +
                    $"S1:{s1}  S2:{s2}  S3:{s3}  Avg:{average}  Result:{resultStatus}");
            }

            _savedMarks[recordKey] = record;

            //  Success output 
            txtMarksOutput.Text =
                (isUpdate ? "Marks updated successfully!" : "Marks processed successfully!") +
                Environment.NewLine +
                "Student ID:   " + record.StudentId + Environment.NewLine +
                "Student Name: " + record.StudentName + Environment.NewLine +
                "Subject 1:    " + record.Subject1 + Environment.NewLine +
                "Subject 2:    " + record.Subject2 + Environment.NewLine +
                "Subject 3:    " + record.Subject3 + Environment.NewLine +
                "Average:      " + record.Average + Environment.NewLine +
                "Final Result: " + record.ResultStatus + Environment.NewLine +
                (isUpdate ? "(Previous record overwritten — audit trail updated.)" : "");
        }

        //  Mark parsing + validation helper 
        /// <summary>
        /// TC-MC-06 / TC-MC-09 : rejects non-numeric and special-character input.
        /// TC-MC-07            : accepts decimal values.
        /// TC-MC-04 / TC-MC-05 : rejects values outside 0–100.
        /// TC-MC-02 / TC-MC-03 : accepts boundary values 0 and 100.
        /// Returns false and shows an error if validation fails.
        /// </summary>
        private static bool TryParseAndValidateMark(string raw, string label, out double value)
        {
            value = 0;

            // TC-MC-06 / TC-MC-09: not a parseable number at all
            if (!double.TryParse(raw.Trim(), out value))
            {
                ShowError($"{label}: Marks field accepts numbers only. '{raw}' is not a valid number.");
                return false;
            }

            // TC-MC-04 / TC-MC-05: out of range
            if (value < 0 || value > 100)
            {
                ShowError($"{label}: Mark must be between 0 and 100. You entered {value}.");
                return false;
            }

            // TC-MC-01 / TC-MC-02 / TC-MC-03 / TC-MC-07: valid
            return true;
        }

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // Clear button
        private void btnClearMarks_Click(object sender, EventArgs e)
        {
            txtMarkStudentId.Clear();
            txtMarkStudentName.Clear();
            txtSubject1.Clear();
            txtSubject2.Clear();
            txtSubject3.Clear();
            txtMarksOutput.Clear();
            txtMarkStudentId.Focus();
        }

        //  Back button
        private void btnBackMarks_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}