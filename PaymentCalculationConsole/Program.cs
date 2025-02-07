﻿using System;
using System.Linq;
using System.Threading;
using Npgsql;
using System.Collections.Generic;
using PaymentCalculation.Model;
using PaymentCalculation.Resources;

namespace PaymentCalculation.PaymentCalculationConsole
{
    class Program
    {
        static DatabaseStorage storage = new DatabaseStorage();
        static Worker currentWorker = null;

        static void Login()
        {
            try
            {
                Console.Write("Enter your login: ");
                currentWorker = storage.FindWorkerByLogin(Console.ReadLine(), false);
                if (currentWorker != null)
                    Console.WriteLine(currentWorker.FirstName + " " + currentWorker.LastName + " - " + currentWorker.Position);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to login: {ex.Message}");
                Login();
            }
        }

        static void AvailableActions()
        {
            string option;
            Console.WriteLine("(0) Exit");
            switch(currentWorker.Position)
            {
                case Position.Supervisor:
                    Console.WriteLine("(1) Add new worker");
                    Console.WriteLine("(2) Add working session");
                    Console.WriteLine("(3) View report for all employees");
                    Console.WriteLine("(4) View report for a specific employee");
                    option = Console.ReadLine();
                    if (option == "0")
                        Environment.Exit(0);
                    else if (option == "1")
                        AddWorker();
                    else if (option == "2")
                    {
                        Console.Write("Enter worker's login: ");
                        string login = Console.ReadLine();
                        AddWorkingSession(login);
                    }
                    else if (option == "3")
                        PrintAllWorkersReport();
                    else if (option == "4")
                    {
                        Console.Write("Enter worker's login: ");
                        string login = Console.ReadLine();
                        PrintWorkerReport(login);
                    }
                    else
                    {
                        Console.WriteLine("Enter correct number!");
                        goto case Position.Supervisor;
                    }    
                    break;
                case Position.LocalEmployee:
                    Console.WriteLine("(1) Add working session");
                    Console.WriteLine("(2) View my sessions");
                    option = Console.ReadLine();
                    if (option == "0")
                        Environment.Exit(0);
                    else if (option == "1")
                    {
                        AddWorkingSession(currentWorker.Login);
                    }
                    else if(option == "2")
                    {
                        PrintWorkerReport(currentWorker.Login);
                    }
                    else
                    {
                        Console.WriteLine("Enter correct number!");
                        goto case Position.LocalEmployee;
                    }
                    break;
                case Position.Freelancer:
                    Console.WriteLine("(1) Add working session");
                    Console.WriteLine("(2) View my sessions");
                    option = Console.ReadLine();
                    if (option == "0")
                        Environment.Exit(0);
                    else if(option == "1")
                    {
                        AddWorkingSession(currentWorker.Login);
                    }
                    else if(option == "2")
                    {
                        PrintWorkerReport(currentWorker.Login);
                    }
                    else
                    {
                        Console.WriteLine("Enter correct number!");
                        goto case Position.Freelancer;
                    }
                    break;
            }
        }

        static void AddWorker()
        {
            try
            {
                Console.Write("Enter worker's login: ");
                string login = Console.ReadLine();
                Console.Write("Enter worker's first name: ");
                string firstName = Console.ReadLine();
                Console.Write("Enter worker's last name: ");
                string lastName = Console.ReadLine();

                PositionChoose:
                Console.WriteLine("Choose worker's position:" +
                    "\n(1) Supervisor" +
                    "\n(2) Local employee" +
                    "\n(3) Freelancer");
                string option = Console.ReadLine();
                Position position;
                if (option == "1")
                    position = Position.Supervisor;
                else if (option == "2")
                    position = Position.LocalEmployee;
                else if (option == "3")
                    position = Position.Freelancer;
                else
                {
                    Console.WriteLine("Enter correct number!");
                    goto PositionChoose;
                }

                Console.WriteLine("Enter worker's salary(not required): ");
                decimal? salary = null;
                string stringSalary = Console.ReadLine();
                if (!string.IsNullOrEmpty(stringSalary))
                    salary = Convert.ToDecimal(stringSalary);

                Worker worker = null;
                switch(position)
                {
                    case Position.Supervisor:
                        worker = new Supervisor(login,firstName,lastName,salary);
                        break;
                    case Position.LocalEmployee:
                        worker = new LocalEmployee(login,firstName,lastName,salary);
                        break;
                    case Position.Freelancer:
                        worker = new Freelancer(login, firstName, lastName, salary);
                        break;
                }
                storage.AddWorker(worker);
                Console.WriteLine("Added new worker.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to add user: {ex.Message}");
            }
            finally
            {
                AvailableActions();
            }
        }

        static void AddWorkingSession(string login)
        {
            try
            {
                storage.FindWorkerByLogin(login, false);
                Console.Write("Enter session's date: ");
                DateTime date = Convert.ToDateTime(Console.ReadLine());
                if(date > DateTime.Now)
                {
                    throw new ArgumentOutOfRangeException("The date was in the future.");
                }
                if(currentWorker.Position == Position.Freelancer && (DateTime.Now.Date - date.Date) > new TimeSpan(2, 0, 0, 0))
                {
                    throw new ArgumentOutOfRangeException("You can not enter a date earlier than 2 days from now.");
                }
                Console.Write("Enter session's time gap: ");
                byte gap = Convert.ToByte(Console.ReadLine());
                if (0 > gap || gap > 24)
                {
                    throw new ArgumentOutOfRangeException("Time period must be between 0 and 24 hours per day.");
                }
                Console.Write("Enter session's comment: ");
                string comment = Console.ReadLine();
                WorkingSession session = new WorkingSession(login, date, gap, comment);
                storage.AddWorkingSession(session);
                Console.WriteLine("Added new session.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to add working session: {ex.Message}");
            }
            finally
            {
                AvailableActions();
            }
        }

        static void PrintWorkerReport(string login)
        {
            try
            {
                Worker worker = storage.FindWorkerByLogin(login, false);

                Console.Write("Enter start date of the report(not required): ");
                DateTime? fromDate = null;
                DateTime? toDate = null;
                string stringFromDate = Console.ReadLine();
                if (!string.IsNullOrEmpty(stringFromDate))
                    fromDate = Convert.ToDateTime(stringFromDate);

                if(fromDate != null)
                {
                    Console.Write("Enter end date of the report(not required): ");
                    string stringToDate = Console.ReadLine();
                    if (!string.IsNullOrEmpty(stringToDate))
                        toDate = Convert.ToDateTime(stringToDate);
                }

                //String interpolation, which displays information about the employee and, depending on the dates values
                Console.WriteLine($"Employee report: {worker.FirstName} {worker.LastName} - {worker.Position}" +
                    //uses ternary operator, that checks if the date is null and based on this builds the string value 
                    $"{(fromDate == null ? "" : toDate == null ? $" for the period from {fromDate} to {DateTime.Now.Date.AddDays(1)}" : $" for the period from {fromDate} to {toDate}")}:");

                List<WorkingSession> workingSessions = storage.GetWorkingSessionsByLogin(login, fromDate, toDate);

                decimal totalPayment = worker.CalculatePayment(workingSessions);
                ushort totalHours = 0;
                foreach (WorkingSession session in workingSessions)
                {
                    totalHours += session.Gap;
                    Console.WriteLine(session.ToString());
                }
                Console.WriteLine($"Result: {totalHours} hours, {totalPayment} to pay.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to get report: {ex.Message}");
            }
            finally
            {
                AvailableActions();
            }
        }

        static void PrintAllWorkersReport()
        {
            try
            {
                Console.Write("Enter start date of the report(not required): ");
                DateTime? fromDate = null;
                DateTime? toDate = null;
                string stringFromDate = Console.ReadLine();
                if (!string.IsNullOrEmpty(stringFromDate))
                    fromDate = Convert.ToDateTime(stringFromDate);

                if(fromDate != null)
                {
                    Console.Write("Enter end date of the report(not required): ");
                    string stringToDate = Console.ReadLine();
                    if (!string.IsNullOrEmpty(stringToDate))
                        toDate = Convert.ToDateTime(stringToDate);
                }

                //String interpolation, which displays information about the employee and, depending on the dates values
                Console.WriteLine($"Employees report " +
                    //uses ternary operator, that checks if the date is null and based on this builds the string value 
                    $"{(fromDate == null ? "" : toDate == null ? $" for the period from {fromDate} to {DateTime.Now.Date.AddDays(1).AddSeconds(-1)}" : $" for the period from {fromDate} to {toDate}")}:");

                List<WorkingSession> allWorkingSessions = storage.GetAllWorkingSessions(fromDate, toDate);
                List<WorkingSession> workerSessions = new List<WorkingSession>();
                string login;
                decimal totalPayment;
                ushort totalHours;
                for(int i = 0; i < allWorkingSessions.Count; i++)
                {
                    login = allWorkingSessions[i].Login;
                    if(workerSessions.FirstOrDefault(x => x.Login == login) != null)
                    {
                        continue;
                    }
                    totalPayment = 0;
                    totalHours = 0;
                    for(int j = i; j < allWorkingSessions.Count; j++)
                    {
                        if(allWorkingSessions[j].Login == login)
                        {
                            workerSessions.Add(allWorkingSessions[j]);
                            totalHours += allWorkingSessions[j].Gap;
                        }
                    }
                    Worker worker = storage.FindWorkerByLogin(login, false);
                    totalPayment = worker.CalculatePayment(workerSessions);
                    Console.WriteLine($"{worker.FirstName} {worker.LastName} - {worker.Position}, worked {totalHours} hours, {totalPayment} to pay.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get report: {ex.Message}");
            }
            finally
            {
                AvailableActions();
            }
        }

        static void Main(string[] args)
        {
            Login();
            AvailableActions();
        }
    }
}
