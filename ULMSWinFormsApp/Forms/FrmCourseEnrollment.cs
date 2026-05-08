using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ULMSWinFormsApp.Models;

namespace ULMSWinFormsApp.Forms
{
    public partial class FrmCourseEnrollment : Form
    {
        //  Course catalogue using YOUR exact course names 
        private static readonly Dictionary<string, (string Name, int Capacity, int SeatsUsed, string[] Prerequisites)> _catalogue = new()
        {
            { "CP1",  ("Programming 1",      30, 10, Array.Empty<string>()) },
            { "WD",   ("Web Development",    30, 10, Array.Empty<string>()) },
            { "ST",   ("Software Testing",   30, 24, new[] { "CP1" })       }, 
            { "DBS",  ("Database System",    30, 30, Array.Empty<string>()) },
        };

        //  Simulated student passed-courses store 
        private static readonly Dictionary<string, HashSet<string>> _passedCourses = new()
        {
            { "S001", new HashSet<string>() },                 
            { "S002", new HashSet<string>() },                  
            { "S003", new HashSet<string> { "CP1" } },          
        };

        //  Active enrolment records 
        // Key: "StudentId|CourseCode"
        private static readonly Dictionary<string, Enrollment> _enrolments = new();

        public FrmCourseEnrollment()
        {
            InitializeComponent();
        }

        private void btnEnroll_Click(object sender, EventArgs e)
        {
            string studentId = txtEnrollStudentId.Text.Trim();
            string studentName = txtEnrollStudentName.Text.Trim();
            string courseName = cmbCourse.Text.Trim();
            string semester = cmbSemester.Text.Trim();

            //  Field presence checks 
            if (string.IsNullOrWhiteSpace(studentId))
            {
                ShowError("Student ID is required.");
                txtEnrollStudentId.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(studentName))
            {
                ShowError("Student Name is required.");
                txtEnrollStudentName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(courseName))
            {
                ShowError("Please select a course.");
                cmbCourse.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(semester))
            {
                ShowError("Please select a semester.");
                cmbSemester.Focus();
                return;
            }

            //  TC-CE-06  Resolve display name → internal course code 
            string courseCode = ResolveCourseCode(courseName);
            if (courseCode == null)
            {
                ShowError($"Course '{courseName}' not found. Please select a valid course.");
                return;
            }

            var course = _catalogue[courseCode];

            //  TC-CE-04  Capacity check 
            if (course.SeatsUsed >= course.Capacity)
            {
                ShowError($"'{course.Name}' is full. No seats available.");
                return;
            }

            //  TC-CE-03  Prerequisite check 
            if (course.Prerequisites.Length > 0)
            {
                _passedCourses.TryGetValue(studentId, out HashSet<string> passed);
                passed ??= new HashSet<string>();

                foreach (string prereq in course.Prerequisites)
                {
                    if (!passed.Contains(prereq))
                    {
                        string prereqName = _catalogue.ContainsKey(prereq)
                            ? _catalogue[prereq].Name
                            : prereq;
                        ShowError($"Prerequisite '{prereqName}' not met. Enrolment denied.");
                        return;
                    }
                }
            }

            //  TC-CE-02  Duplicate enrolment check 
            string enrolKey = $"{studentId}|{courseCode}";
            if (_enrolments.ContainsKey(enrolKey))
            {
                ShowError($"You are already enrolled in '{course.Name}'.");
                return;
            }

            //  TC-CE-01 / TC-CE-05 / TC-CE-07  Record the enrolment 
            Enrollment enrollment = new Enrollment
            {
                StudentId = studentId,
                StudentName = studentName,
                CourseName = courseName,   
                Semester = semester
            };

            _enrolments[enrolKey] = enrollment;

            // Update seat count
            var (Name, Capacity, SeatsUsed, Prerequisites) = _catalogue[courseCode];
            _catalogue[courseCode] = (Name, Capacity, SeatsUsed + 1, Prerequisites);

            //  Success output 
            txtEnrollmentOutput.Text =
                "Enrollment completed successfully!" + Environment.NewLine +
                "Student ID:   " + enrollment.StudentId + Environment.NewLine +
                "Student Name: " + enrollment.StudentName + Environment.NewLine +
                "Course:       " + enrollment.CourseName + Environment.NewLine +
                "Semester:     " + enrollment.Semester;
        }

        //  TC-CE-07  Drop a course so the student can re-enrol 
        public bool DropCourse(string studentId, string courseCode)
        {
            string enrolKey = $"{studentId}|{courseCode}";

            if (!_enrolments.ContainsKey(enrolKey))
                return false;

            _enrolments.Remove(enrolKey);

            if (_catalogue.TryGetValue(courseCode, out var c))
                _catalogue[courseCode] = (c.Name, c.Capacity, Math.Max(0, c.SeatsUsed - 1), c.Prerequisites);

            return true;
        }

        //  Helpers 

        /// <summary>
        /// Maps the combo display text back to a catalogue key.
        /// Handles both exact code match and full display-name match.
        /// Returns null for anything not in the catalogue (TC-CE-06).
        /// </summary>
        private static string ResolveCourseCode(string input)
        {
            if (_catalogue.ContainsKey(input))
                return input;

            foreach (var kvp in _catalogue)
                if (string.Equals(kvp.Value.Name, input, StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;

            return null;
        }

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Enrolment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        
        private void btnClearEnrollment_Click(object sender, EventArgs e)
        {
            txtEnrollStudentId.Clear();
            txtEnrollStudentName.Clear();
            cmbCourse.SelectedIndex = -1;
            cmbSemester.SelectedIndex = -1;
            txtEnrollmentOutput.Clear();
            txtEnrollStudentId.Focus();
        }

        private void btnBackEnrollment_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}