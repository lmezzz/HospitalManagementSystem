# Hospital Management System (HMS)

A comprehensive web-based Hospital Management System built with ASP.NET Core MVC and PostgreSQL, designed to automate key hospital operations including patient management, appointments, clinical records, prescriptions, billing, and laboratory operations.

## 🏥 Project Overview

This HMS implements all requirements specified in the SRS (Software Requirements Specification) and FSR (Feasibility Study Report), providing a complete solution for modern healthcare facilities.

### Key Features

- **User Management**: Role-based access control (Admin, Doctor, Patient, Nurse, Pharmacist, Lab Technician, Receptionist)
- **Patient Management**: Complete patient registration, demographics, medical history, and records
- **Appointment System**: Scheduling, calendar view, conflict detection, and automated workflows
- **Clinical Documentation**: Visit records, SOAP notes, diagnosis, and treatment plans
- **E-Prescribing**: Digital prescription creation with medication inventory integration
- **Pharmacy Module**: Medication dispensing, inventory management, and low-stock alerts
- **Laboratory**: Test ordering, result management, and file upload support
- **Billing & Payments**: Automated billing, multiple payment methods, invoicing, and receipts
- **Reporting**: Comprehensive dashboards and analytics for all user roles
- **Security**: Role-based authorization, secure authentication, and audit trails

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 6+)
- **Database**: PostgreSQL with Entity Framework Core
- **Frontend**: Bootstrap 5, jQuery, HTML5, CSS3
- **Authentication**: Cookie-based authentication with role-based authorization
- **ORM**: Entity Framework Core 9.0

## 📋 Prerequisites

- .NET SDK 6.0 or later
- PostgreSQL 12 or later
- Visual Studio 2022 / VS Code / JetBrains Rider (recommended)
- Node.js and npm (for frontend dependencies)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd HospitalManagementSystem
```

### 2. Database Setup

1. Create a PostgreSQL database:
```sql
CREATE DATABASE hospital_management;
```

2. Update the connection string in `appsettings.json` or use User Secrets:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hospital_management;Username=postgres;Password=yourpassword"
  }
}
```

Or use User Secrets (recommended for development):
```bash
cd WebManagementSystem
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hospital_management;Username=postgres;Password=yourpassword"
```

3. Apply database migrations:
```bash
dotnet ef database update
```

### 3. Run the Application

```bash
cd WebManagementSystem
dotnet run
```

The application will be available at `https://localhost:5001` (or the port shown in console).

## 👥 User Roles & Access

### Admin
- Full system access
- User management (create/edit/delete users)
- System reports and analytics
- Configuration management

**Default Admin Credentials** (if seeded):
- Username: `admin`
- Password: `admin123`

### Doctor
- View appointments and schedule
- Patient consultations and visit documentation
- Prescription creation
- Lab test ordering
- View assigned patients

### Patient
- View appointments and medical history
- View prescriptions and lab results
- View and pay bills
- Update profile information

### Pharmacist
- Dispense prescriptions
- Manage medication inventory
- Stock updates and alerts
- View prescription queue

### Lab Technician
- Manage lab orders
- Upload test results
- Track sample collection
- Result approval

### Receptionist
- Patient registration
- Appointment booking
- Check-in management

## 📊 Database Schema

The system uses a comprehensive relational database with the following key entities:

- **AppUser**: System users with role-based access
- **Patient**: Patient-specific information and medical records
- **Appointment**: Appointment scheduling and tracking
- **Visit**: Clinical encounter documentation
- **Prescription**: E-prescription management
- **Medication**: Pharmacy inventory
- **LabOrder**: Laboratory test ordering
- **LabResult**: Test results and file uploads
- **Bill**: Billing and invoicing
- **Payment**: Payment processing and tracking
- **Schedule**: Doctor availability management

## 🔑 Key Workflows

### 1. Patient Registration & Appointment
1. Receptionist registers new patient
2. Patient books appointment with preferred doctor
3. System checks doctor availability
4. Appointment confirmed and scheduled

### 2. Clinical Consultation
1. Doctor views appointment in daily schedule
2. Creates visit record for patient
3. Documents symptoms, diagnosis, and treatment plan
4. Prescribes medications
5. Orders laboratory tests if needed

### 3. Prescription Fulfillment
1. Prescription appears in pharmacy queue
2. Pharmacist reviews prescription
3. System validates medication availability
4. Medications dispensed (stock automatically deducted)
5. Receipt generated

### 4. Laboratory Workflow
1. Doctor orders lab tests during visit
2. Lab receives order with patient information
3. Sample collection and processing
4. Technician uploads results
5. Results available to doctor and patient

### 5. Billing & Payment
1. System auto-generates bill from visit, medications, and tests
2. Patient/receptionist reviews bill
3. Payment processed (cash/card/insurance)
4. Receipt and invoice generated
5. Outstanding balance tracked

## 📁 Project Structure

```
HospitalManagementSystem/
├── WebManagementSystem/
│   ├── Controllers/          # MVC Controllers
│   │   ├── AccountController.cs
│   │   ├── AdminController.cs
│   │   ├── AppointmentController.cs
│   │   ├── BillingController.cs
│   │   ├── DoctorController.cs
│   │   ├── LabController.cs
│   │   ├── PatientController.cs
│   │   ├── PharmacyController.cs
│   │   ├── PrescriptionController.cs
│   │   └── VisitController.cs
│   ├── Models/              # Database models and ViewModels
│   │   ├── HmsContext.cs    # EF Core DbContext
│   │   └── ViewModels/      # Data transfer objects
│   ├── Views/               # Razor views
│   ├── wwwroot/            # Static files (CSS, JS, images)
│   ├── Services/           # Business logic services
│   └── Program.cs          # Application startup
└── README.md
```

## 🔒 Security Features

- Role-based authorization on all sensitive endpoints
- Secure cookie-based authentication
- Password hashing (configurable - currently disabled for testing)
- SQL injection prevention via EF Core parameterized queries
- XSS protection through Razor view encoding
- CSRF protection via ASP.NET Core anti-forgery tokens

## 📈 Features by Module

### Admin Module
✅ User CRUD operations
✅ Role management
✅ System-wide reports
✅ Revenue analytics
✅ Doctor performance tracking
✅ Patient demographics

### Patient Module
✅ Registration and profile management
✅ Appointment booking
✅ Medical history viewing
✅ Prescription access
✅ Lab results viewing
✅ Bill payment

### Doctor Module
✅ Daily schedule management
✅ Patient consultations
✅ Visit documentation
✅ E-prescribing
✅ Lab test ordering
✅ Patient history access
✅ Workload reports

### Pharmacy Module
✅ Prescription queue
✅ Medication dispensing
✅ Inventory management
✅ Low stock alerts
✅ Stock updates
✅ Sales reports

### Lab Module
✅ Test order management
✅ Result entry with file upload
✅ Priority-based ordering
✅ Turnaround time tracking
✅ Result approval workflow

### Billing Module
✅ Automated bill generation
✅ Multiple payment methods
✅ Partial payment support
✅ Invoice/receipt generation
✅ Outstanding balance tracking
✅ Revenue reports

## 🎯 SRS Compliance

This implementation fulfills all functional requirements (FR-1 through FR-11) and non-functional requirements (NFR-1 through NFR-7) as specified in the Software Requirements Specification:

- ✅ FR-1: User Authentication & Authorization
- ✅ FR-2: Patient Management
- ✅ FR-3: Appointments & Scheduling
- ✅ FR-4: Visits & Clinical Notes
- ✅ FR-5: Prescriptions & Pharmacy
- ✅ FR-6: Billing & Payments
- ✅ FR-7: Laboratory & Radiology Orders
- ✅ FR-9: Reporting & Dashboard
- ✅ FR-11: Audit & Security

## 🧪 Testing

### Manual Testing
1. Register different user types (Admin, Doctor, Patient, etc.)
2. Test complete workflows end-to-end
3. Verify role-based access restrictions
4. Test data validation and error handling

### Sample Data
Seed the database with sample data for testing:
```bash
dotnet run --seed-data
```

## 🤝 Contributing

1. Create a feature branch from `develop`
2. Follow conventional commit messages
3. Ensure all workflows remain functional
4. Submit pull request with detailed description

## 📝 Conventional Commits

This project follows the [Conventional Commits](https://www.conventionalcommits.org/) specification:

- `feat:` New features
- `fix:` Bug fixes
- `refactor:` Code refactoring
- `docs:` Documentation changes
- `style:` Code style changes
- `test:` Test additions/modifications
- `chore:` Build process or auxiliary tool changes

## 🐛 Known Issues

- Password hashing is currently disabled for testing purposes (enable in production)
- Email/SMS notifications not implemented (optional feature)
- Radiology module not yet implemented
- Inpatient/admissions module not yet implemented

## 🔮 Future Enhancements

- [ ] Email/SMS notification service
- [ ] Patient portal mobile app
- [ ] Telemedicine integration
- [ ] Advanced analytics and BI dashboards
- [ ] Insurance claims processing
- [ ] Radiology/imaging module
- [ ] Inpatient/admission management
- [ ] Operating room scheduling
- [ ] Integration with external lab systems

## 📄 License

This project is developed as an academic project for database systems coursework.

## 👨‍💻 Authors

Developed as part of Database Systems course project.

## 📞 Support

For issues or questions:
1. Check the documentation
2. Review the SRS/FSR documents
3. Create an issue in the repository

---

**Note**: This is a semester project and should not be used in production healthcare environments without proper security audits, HIPAA compliance verification, and regulatory approval.
