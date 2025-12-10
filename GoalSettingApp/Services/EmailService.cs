using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using GoalSettingApp.Models;
using GoalSettingApp.Helpers;

namespace GoalSettingApp.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailSettings settings, IWebHostEnvironment environment, ILogger<EmailService> logger)
        {
            _settings = settings;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Sends a task reminder email to the user
        /// </summary>
        public async Task<bool> SendTaskReminderAsync(string toEmail, string toName, string taskTitle, string taskDescription, DateTime dueDate, string reminderType)
        {
            try
            {
                // Load the HTML template
                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "task-reminder.html");
                
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template not found at {Path}", templatePath);
                    return false;
                }

                var htmlTemplate = await File.ReadAllTextAsync(templatePath);

                // Replace placeholders with actual values (HTML-encoded for security)
                htmlTemplate = htmlTemplate
                    .Replace("{{to_name}}", HtmlEncoder.Encode(toName))
                    .Replace("{{task_title}}", HtmlEncoder.Encode(taskTitle))
                    .Replace("{{task_description}}", HtmlEncoder.Encode(string.IsNullOrEmpty(taskDescription) ? "No description provided" : taskDescription))
                    .Replace("{{due_date}}", HtmlEncoder.Encode(dueDate.ToString("dddd, MMMM dd, yyyy")))
                    .Replace("{{reminder_type}}", HtmlEncoder.Encode(reminderType))
                    .Replace("{{app_url}}", Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5125");

                // Create the email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = GetSubjectLine(reminderType, taskTitle);

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlTemplate
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send the email
                using var client = new SmtpClient();
                
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Task reminder email sent to {Email} for task: {Task}", toEmail, taskTitle);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send task reminder email to {Email}", toEmail);
                return false;
            }
        }

        private static string GetSubjectLine(string reminderType, string taskTitle)
        {
            return reminderType.ToLower() switch
            {
                "overdue" => $"⚠️ Overdue Task: {taskTitle}",
                "today" => $"📅 Task Due Today: {taskTitle}",
                "tomorrow" => $"🔔 Task Due Tomorrow: {taskTitle}",
                _ => $"Task Reminder: {taskTitle}"
            };
        }

        /// <summary>
        /// Sends a daily reminder email with all pending tasks
        /// </summary>
        public async Task<bool> SendDailyReminderAsync(string toEmail, string toName, List<Goal> pendingTasks, bool isMorning)
        {
            try
            {
                if (pendingTasks == null || pendingTasks.Count == 0)
                {
                    _logger.LogInformation("No pending tasks for {Email}, skipping daily reminder", toEmail);
                    return true;
                }

                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "daily-reminder.html");

                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Daily reminder template not found at {Path}", templatePath);
                    return false;
                }

                var htmlTemplate = await File.ReadAllTextAsync(templatePath);

                // Build task list HTML
                var taskListHtml = BuildTaskListHtml(pendingTasks);

                // Set greeting based on time of day
                var greetingEmoji = isMorning ? "☀️" : "🌙";
                var greetingText = isMorning ? "Good Morning!" : "Good Evening!";
                var introMessage = isMorning
                    ? $"Here's your morning overview of {pendingTasks.Count} task(s) to focus on today:"
                    : $"Here's your evening recap of {pendingTasks.Count} task(s) still pending:";
                var closingMessage = isMorning
                    ? "Have a productive day! 💪"
                    : "Rest well and tackle these tomorrow! 🌟";

                htmlTemplate = htmlTemplate
                    .Replace("{{to_name}}", HtmlEncoder.Encode(toName))
                    .Replace("{{greeting_emoji}}", greetingEmoji)
                    .Replace("{{greeting_text}}", greetingText)
                    .Replace("{{intro_message}}", HtmlEncoder.Encode(introMessage))
                    .Replace("{{task_list}}", taskListHtml) // Already encoded in BuildTaskListHtml
                    .Replace("{{closing_message}}", closingMessage)
                    .Replace("{{app_url}}", Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5125");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = isMorning
                    ? $"☀️ Morning Reminder: {pendingTasks.Count} task(s) awaiting you"
                    : $"🌙 Evening Recap: {pendingTasks.Count} task(s) still pending";

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlTemplate };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Daily {TimeOfDay} reminder sent to {Email} with {Count} tasks",
                    isMorning ? "morning" : "evening", toEmail, pendingTasks.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily reminder email to {Email}", toEmail);
                return false;
            }
        }

        private static string BuildTaskListHtml(List<Goal> tasks)
        {
            var html = new System.Text.StringBuilder();

            foreach (var task in tasks)
            {
                var priorityColor = task.Priority switch
                {
                    PriorityLevel.High => "#ef4444",
                    PriorityLevel.Medium => "#f59e0b",
                    PriorityLevel.Low => "#22c55e",
                    _ => "#6b7280"
                };

                var dueDateText = task.DueDate.HasValue
                    ? task.DueDate.Value.ToString("MMM dd, yyyy")
                    : "No due date";

                var isOverdue = task.DueDate.HasValue && task.DueDate.Value.Date < DateTime.Today;
                var dueDateStyle = isOverdue ? "color: #ef4444; font-weight: bold;" : "color: #6366f1;";

                // HTML-encode user-provided content
                var encodedTitle = HtmlEncoder.Encode(task.Title);
                var encodedDescription = HtmlEncoder.Encode(string.IsNullOrEmpty(task.Description) ? "No description" : task.Description);
                
                html.Append($@"
                <div style=""background-color: #f9fafb; border-left: 4px solid {priorityColor}; border-radius: 4px; padding: 15px; margin: 10px 0;"">
                    <h3 style=""color: #1f2937; margin: 0 0 8px 0; font-size: 16px;"">{encodedTitle}</h3>
                    <p style=""color: #6b7280; margin: 0 0 10px 0; font-size: 14px;"">{encodedDescription}</p>
                    <div style=""display: flex; gap: 15px; font-size: 12px;"">
                        <span style=""{dueDateStyle}"">📅 {dueDateText}{(isOverdue ? " (Overdue)" : "")}</span>
                        <span style=""color: {priorityColor}; font-weight: bold;"">● {task.Priority} Priority</span>
                    </div>
                </div>");
            }

            return html.ToString();
        }
    }
}