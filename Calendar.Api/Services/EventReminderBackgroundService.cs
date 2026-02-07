using Calendar.Api.Application;
using Calendar.Api.Models;

namespace Calendar.Api.Services
{
    public class EventReminderBackgroundService : BackgroundService
    {
        private readonly ILogger<EventReminderBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public EventReminderBackgroundService(
            ILogger<EventReminderBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Reminder Background Service is starting.");

            // חישוב הזמן עד לשעה 8:00 בבוקר
            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
            
            // אם כבר עברנו את השעה 8:00 היום, קבע למחר
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var timeUntilFirstRun = scheduledTime - now;

            _logger.LogInformation($"First email check will run at: {scheduledTime}");

            // הגדר טיימר שירוץ כל 24 שעות, מתחיל בשעה 8:00 בבוקר
            _timer = new Timer(
                CheckAndSendReminders,
                null,
                timeUntilFirstRun,
                TimeSpan.FromHours(24));

            // אופציה: אם את רוצה לבדוק מיד בהרצה (לצורך בדיקה):
            // הסר את ההערה מהשורה הבאה
             CheckAndSendReminders(null);

            return Task.CompletedTask;
        }

        private void CheckAndSendReminders(object? state)
        {
            _logger.LogInformation("Checking for events to send reminders...");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    // טען אירועים ומשתמשים
                    var events = DataService.LoadEvents();
                    var users = DataService.LoadUsers();

                    var today = DateTime.Today;

                    // מצא את כל האירועים שמתחילים היום
                    var todayEvents = events.Where(e => e.StartDate.Date == today).ToList();

                    _logger.LogInformation($"Found {todayEvents.Count} events for today ({today:dd/MM/yyyy})");

                    foreach (var eventItem in todayEvents)
                    {
                        try
                        {
                            // מצא את המשתמש של האירוע
                            var user = users.FirstOrDefault(u => u.UserId == eventItem.UserId);

                            if (user == null)
                            {
                                _logger.LogWarning($"User not found for event {eventItem.EventId}");
                                continue;
                            }

                            if (string.IsNullOrEmpty(user.Email))
                            {
                                _logger.LogWarning($"User {user.UserId} has no email address");
                                continue;
                            }

                            // שלח מייל תזכורת
                            emailService.SendEventReminderAsync(
                                user.Email,
                                user.FirstName ?? user.UserId,
                                eventItem.Title,
                                eventItem.Description,
                                eventItem.StartDate
                            ).Wait();

                            _logger.LogInformation($"Reminder sent for event: {eventItem.Title} to {user.Email}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error sending reminder for event {eventItem.EventId}: {ex.Message}");
                        }
                    }

                    _logger.LogInformation("Finished checking events for today.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CheckAndSendReminders: {ex.Message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event Reminder Background Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
