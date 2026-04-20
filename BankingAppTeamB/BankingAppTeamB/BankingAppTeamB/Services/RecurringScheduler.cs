using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using BankingAppTeamB.Services;

namespace BankingAppTeamB.Services
{
    public class RecurringScheduler : IRecurringScheduler
    {
        private readonly IRecurringPaymentService recurringPaymentService;
        private readonly IExchangeService exchangeService;
        private readonly System.Timers.Timer timer;

        public RecurringScheduler(IRecurringPaymentService recurringPaymentService, IExchangeService exchangeService, System.Timers.Timer timer)
        {
            this.recurringPaymentService = recurringPaymentService;
            this.exchangeService = exchangeService;
            this.timer = timer;
            this.timer.Elapsed += OnTick;
        }

        /// <summary>Starts the underlying timer so background jobs run on every tick interval (default 30 seconds).</summary>
        public void Start()
        {
            timer.Start();
            Debug.WriteLine("[RecurringScheduler] Scheduler started.");
        }

        /// <summary>Stops the timer, halting all background job execution.</summary>
        public void Stop()
        {
            timer.Stop();
            Debug.WriteLine("[RecurringScheduler] Scheduler stopped.");
        }

        /// <summary>Timer callback: processes due recurring payments and checks rate alerts on each tick, swallowing and logging any exceptions so the scheduler keeps running.</summary>
        private void OnTick(object? sender, ElapsedEventArgs e)
        {
            Debug.WriteLine($"[RecurringScheduler] Tick at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

            try
            {
                recurringPaymentService.ProcessDuePayments();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"[RecurringScheduler] ProcessDuePayments failed: {exception.Message}");
            }

            try
            {
                exchangeService.CheckRateAlerts();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"[RecurringScheduler] CheckRateAlerts failed: {exception.Message}");
            }
        }
    }
}