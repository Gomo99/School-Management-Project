# SchoolProject – Assessment Management System

A secure, multi‑role web application built with **ASP.NET Core MVC** and **Entity Framework Core** to help students, lecturers, and administrators manage modules, enrolments, and assessments. The system replaces manual tracking with a centralised digital platform and supports role‑based dashboards, two‑factor authentication, real‑time messaging, notifications, and reporting.

---

## Features

### Authentication & Security
- **Role‑Based Access Control** – Four roles: **Administrator**, **Lecturer**, **Student**, and **Parent**.
- **BCrypt Password Hashing** – All passwords are hashed; legacy plain‑text passwords are upgraded automatically on successful login.
- **Account Lockout** – After 5 consecutive failed attempts, the account is locked for 15 minutes.
- **Forgot/Reset Password** – A time‑limited PIN (5 minutes) is emailed to the user.
- **Two‑Factor Authentication (TOTP)** – Opt‑in 2FA via authenticator apps; QR code setup, recovery codes, device trust (“Remember this device” for 30 days).
- **Google Sign‑In** – External login via Google; new users are auto‑registered with default Student role.
- **Email Verification** – New accounts can be verified via a link sent to their email.
- **Persistent Login** – Authentication cookie lasts 30 days with sliding expiration.

### Administrator Module
- **User Management** – Create, edit, and soft‑delete (deactivate/reactivate) user accounts for any role. Assign roles (Student, Lecturer, Administrator, Parent).
- **Module Management** – Create, edit, and deactivate modules (ModuleName, Duration, ModuleType).
- **Lecturer‑Module Assignment** – Assign a lecturer to one or multiple modules; change lecturer for a module (deactivates old, creates new assignment); edit or deactivate assignments.
- **Student Enrolment** – Enrol a student into a specific lecturer‑module pair; edit or deactivate enrolments.
- **Assessment Types** – CRUD for assessment type descriptions (e.g., Test, Exam, Assignment).
- **Parent‑Student Linking** – Administratively link a parent account to a student account.
- **Reports**  
  - **User Report** – Search users by name or number; filter by role.  
  - **Module Report** – View all modules; select a module to see its lecturer, student count, and module type.  
  - **Assessment Report** – Filter assessments by status (Complete, Missed, etc.), assessment type, and date range.
- **Notifications** – In‑app notifications sent to users on key events (user creation, enrolment changes, etc.).

### Lecturer Module
- **Dashboard** – Overview of assigned modules, student count, and assessment statistics.
- **Assessment Management** – Create, edit, and soft‑delete assessments for students enrolled in their own modules. Choose assessment type and due date.
- **Student Lookup** – View all students across their active modules; drill into a specific student’s assessment statuses (completed, missed, etc.).
- **Assessment Report** – Filter their own assessments by status, type, and date range.
- **Assessment Type CRUD** – Manage custom assessment type descriptions.
- **Notifications** – Receive alerts when assigned to new modules or when students complete assessments.

### Student Module
- **Dashboard** – Displays modules the student is enrolled in, with their current assessment statuses and lecturer information.
- **Assessment Tracking** – View detailed assessments per module; mark them as **Completed**, **Missed**, or **Rescheduled** (with a new date). Due dates and current statuses are displayed.
- **Profile Management** – Edit own details, change password, enable/disable 2FA.
- **Notifications** – In‑app notification dropdown showing recent alerts; ability to mark all as read or individual.

### Parent Module
- **Dashboard** – Lists all children linked to the parent account.
- **Child Modules** – View the enrolled modules for a selected child, including module name and assigned lecturer.
- **Child Assessments** – View all assessments for a specific student‑module, with due dates and current statuses, allowing parents to monitor academic progress.

### Messaging System
- **Inbox & Sent** – View received and sent messages with read/unread indicators.
- **Compose** – Send new messages to any active user; support for **Reply** (pre‑fills recipient) and **Forward** (pre‑fills content).
- **Real‑time Typing Indicators** – SignalR notifies users when the other party is typing.
- **Message Management** – Delete messages (soft delete), mark as read/unread.
- **Unread Count** – Real‑time badge showing unread messages via AJAX.

### Notifications System
- **In‑app Notifications** – Real‑time alerts via SignalR for events like new assessments, enrolment changes, password changes.
- **Background Service** – `AssessmentDeadlineNotificationService` sends reminders when assessments are due soon.
- **API Endpoints** – RESTful endpoints to fetch latest notifications, mark individual or all as read.

### Technical Highlights
- **Real‑time Communication** – SignalR hubs (`MessageHub`) for instant messaging, typing indicators, and live notifications.
- **Email Service** – Template‑based emails for password reset, verification, welcome, and recovery codes.
- **PDF Generation** – iTextSharp used for downloading user profiles as PDF.
- **Device Trust** – Remembered devices bypass 2FA for 30 days; revocable by the user.
- **3‑Tier Architecture** – Follows a layered design with Controllers, Services, and Data access.

---

## User Roles

| Role            | Permissions                                                                                     |
|-----------------|-------------------------------------------------------------------------------------------------|
| **Administrator** | Full system management: users, modules, lecturer‑module assignments, student enrolments, assessment types, reports, parent links. |
| **Lecturer**     | Create assessments for own modules, view student submissions, view student reports, manage assessment types. |
| **Student**      | View enrolled modules, track assessments (mark completed/missed/rescheduled), manage profile.   |
| **Parent**       | View linked students’ modules and assessments to monitor academic progress.                     |

---

## Technology Stack

- **Backend:** ASP.NET Core MVC (.NET 6/7/8), Entity Framework Core
- **Database:** SQL Server (LocalDB, Express, or full edition)
- **Frontend:** Razor Views with Bootstrap 5, jQuery, SignalR
- **Authentication:** ASP.NET Core Cookie Authentication, Google OAuth 2.0
- **Two‑Factor Authentication:** TOTP (RFC 6238) with SHA‑1, BCrypt hashing
- **Email:** Custom `EmailService` with HTML templates
- **PDF Generation:** iTextSharp
- **Real‑time Communication:** SignalR (`MessageHub`)
- **Background Jobs:** Hosted service for assessment deadline notifications
- **Messaging:** Internal message system (inbox, sent, compose, reply/forward)

---

## Setup & Installation

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or later)
- SQL Server (LocalDB or higher)
- Visual Studio 2022 / VS Code / Rider
- An SMTP email service (e.g., SendGrid, Gmail SMTP) for email functionality.
- Google Cloud Console project (for Google Sign‑In) – obtain Client ID and Secret.

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/school-project.git
   cd school-project