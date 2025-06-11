# MyNUnitTestProject

## Overview
MyNUnitTestProject is a .NET application that demonstrates the use of NUnit for unit testing. This project includes a main application and a separate test project to ensure the functionality of the application through automated tests.

## Project Structure
```
MyNUnitTestProject
├── src
│   └── MyNUnitTestProject.csproj
├── tests
│   ├── MyNUnitTestProject.Tests.csproj
│   └── UnitTests
│       └── SampleTests.cs
└── README.md
```

## Setup Instructions

1. **Clone the Repository**
   ```
   git clone <repository-url>
   cd MyNUnitTestProject
   ```

2. **Restore Dependencies**
   Run the following command to restore the project dependencies:
   ```
   dotnet restore
   ```

3. **Build the Project**
   Build the project using:
   ```
   dotnet build
   ```

4. **Run Tests**
   To execute the tests, navigate to the `tests` directory and run:
   ```
   dotnet test
   ```

## Usage
This project serves as a template for creating .NET applications with unit tests using NUnit. You can modify the `SampleTests.cs` file to add your own test cases and ensure your application behaves as expected.

## Contributing
Contributions are welcome! Please feel free to submit a pull request or open an issue for any enhancements or bug fixes.