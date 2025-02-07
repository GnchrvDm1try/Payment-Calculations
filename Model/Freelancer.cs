﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentCalculation.Model
{
    public class Freelancer : Worker
    {
        public decimal PaymentPerHour { get; set; }
       
        public Freelancer(string login, string firstName, string lastName, decimal? paymentPerHour = null) : base(login, firstName, lastName)
        {
            if (paymentPerHour != null)
                PaymentPerHour = (decimal)paymentPerHour;
            else
                PaymentPerHour = Config.FREELANCER_HOUR_PAYMENT;
            Position = Position.Freelancer;
        }

        public override decimal CalculatePayment(List<WorkingSession> sessions)
        {
            decimal totalPayment = 0;
            try
            {
                foreach(WorkingSession session in sessions)
                {
                    if(session.Login == Login)
                    {
                        totalPayment += PaymentPerHour * session.Gap;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Не удалось посчитать выплату. " + ex.Message);
            }
            return totalPayment;
        }
    }
}
