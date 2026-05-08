 Student Information System - Test Suite

This repository contains the corrected code and a comprehensive test suite for validating the Student Information System (SIS).  
The system covers login validation, course enrolment, marks capture, average calculation, and report generation.
📌 Features Tested

 1. [Login Validation]
- Valid and invalid login attempts
- Empty field validation
- SQL injection protection
- Brute force lockout
- Case sensitivity checks
- Admin vs student dashboard redirection

 2. [Course Enrolment]
- Successful enrolment with vacancies
- Duplicate enrolment prevention
- Prerequisite enforcement
- Full course blocking
- Multiple course enrolments
- Invalid course code handling
- Drop and re-enrol scenarios

3. [Marks Capture]
-Valid numeric marks (0–100)
- Boundary values (0 and 100)
- Out-of-range values
- Negative marks
- Alphabetic and special character rejection
- Decimal marks handling
- Empty field validation
- Editing existing marks

4. [Average Calculation]
- Maximum and minimum averages
- Mixed marks calculation
- Single subject averages
- Boundary inclusion (0 with high marks)
- Decimal rounding consistency
- Handling missing marks
- Recalculation after edits

 5. [Report Generation]
- Correct student details
- Accurate average display
- Handling missing marks
- Performance testing (single vs cohort)
- Access control (student cannot view another’s report)
- Export to PDF correctness
- Updated marks reflected in reports
- Reports for students with no marks

 ✅ Success Criteria

- All login, enrolment, marks, average, and report tests must Pass.
- No security vulnerabilities (SQL injection, brute force bypass).
- Reports must be accurate, performant, and secure.
- Validation must prevent incorrect or duplicate data entry.

---
