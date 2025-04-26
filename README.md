# SCIS - Student Club Information System

SCIS is a comprehensive web application for managing student clubs, events, and announcements within an educational institution. The system supports different user roles (Admin, Club Manager, and regular Users) with specific permissions and functionalities.

## Features

- **User Management**: Registration, authentication, and role-based access control
- **Club Management**: Create, join, and manage clubs
- **Event Management**: Create and manage events, register for events
- **Announcement System**: Post and view announcements
- **Admin Dashboard**: Comprehensive administration tools
- **Responsive Design**: Works on desktop and mobile devices

## Requirements

- .NET 9.0 SDK
- SQL Server (LocalDB or full SQL Server)
- Visual Studio 2022 or later (recommended) or Visual Studio Code

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/Maharshi-24/SCIS.git
cd SCIS
```

### 2. Database Setup

The application uses Entity Framework Core with Code First approach. To set up the database:

1. Open the `appsettings.json` file and verify the connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SCIS;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```
   
2. Update the connection string if you're using a different SQL Server instance.

3. Apply migrations to create the database:
   ```bash
   dotnet ef database update
   ```

### 3. Run the Application

```bash
dotnet run
```

Or open the solution in Visual Studio and press F5 to run.

### 4. Default Admin Account

The system is pre-configured with an admin account:
- Email: admin@gmail.com
- Password: admin

## User Roles

### Admin
- Manage all users, clubs, and events
- Create announcements
- Approve/reject club membership requests
- Reset user passwords

### Club Manager (Club President)
- Manage their club details
- Create and manage club events
- Approve/reject membership requests for their club

### Regular User
- View and join clubs
- Register for events
- View announcements and club information

## Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- Identity Framework for authentication
- Bootstrap 5 for UI
- jQuery and JavaScript for client-side functionality
- Font Awesome for icons

## License

This project is licensed under the MIT License - see the LICENSE file for details.
