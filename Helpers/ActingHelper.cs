using Ideku.Models.Entities;

namespace Ideku.Helpers
{
    /// <summary>
    /// Helper class for User acting period calculations and validations
    /// Contains business logic for acting duration, status, and period management
    /// </summary>
    public static class ActingHelper
    {
        /// <summary>
        /// Check if user is currently in active acting period
        /// Validates that acting is enabled and current date is within acting period
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>True if user is currently acting, false otherwise</returns>
        public static bool IsCurrentlyActing(User user)
        {
            if (user == null) return false;

            if (!user.IsActing || !user.ActingStartDate.HasValue || !user.ActingEndDate.HasValue)
                return false;

            var now = DateTime.Now;
            return now >= user.ActingStartDate.Value && now < user.ActingEndDate.Value;
        }

        /// <summary>
        /// Get remaining days in acting period
        /// Returns 0 if not currently acting or acting period has ended
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>Number of days remaining in acting period</returns>
        public static int GetActingDaysRemaining(User user)
        {
            if (user == null) return 0;

            if (!IsCurrentlyActing(user) || !user.ActingEndDate.HasValue)
                return 0;

            var remainingTime = user.ActingEndDate.Value - DateTime.Now;
            return Math.Max(0, (int)Math.Ceiling(remainingTime.TotalDays));
        }

        /// <summary>
        /// Get total acting period duration in days
        /// Calculates duration between start and end dates regardless of current status
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>Total duration in days, 0 if dates are not set</returns>
        public static int GetActingDurationInDays(User user)
        {
            if (user == null) return 0;

            if (!user.ActingStartDate.HasValue || !user.ActingEndDate.HasValue)
                return 0;

            return (int)Math.Ceiling((user.ActingEndDate.Value - user.ActingStartDate.Value).TotalDays);
        }

        /// <summary>
        /// Get formatted acting status text for UI display
        /// Returns human-readable status with dates and remaining time
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>Formatted status text</returns>
        public static string GetActingStatusText(User user)
        {
            if (user == null) return "N/A";

            if (!IsCurrentlyActing(user))
                return "Regular";

            var daysLeft = GetActingDaysRemaining(user);
            var endDate = user.ActingEndDate!.Value.ToString("MMM dd, yyyy");

            if (daysLeft <= 0)
                return $"Acting period ended ({endDate})";

            return $"Acting until {endDate} ({daysLeft} {(daysLeft == 1 ? "day" : "days")} left)";
        }

        /// <summary>
        /// Get role display text with acting indicator
        /// Shows current role with indication if it's an acting role
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>Formatted role text with acting indicator</returns>
        public static string GetRoleDisplayText(User user)
        {
            if (user?.Role == null) return "N/A";

            if (IsCurrentlyActing(user))
            {
                var originalRole = user.CurrentRole?.RoleName ?? "Unknown";
                return $"{user.Role.RoleName} (Acting from {originalRole})";
            }
            return user.Role.RoleName;
        }

        /// <summary>
        /// Check if acting period is about to expire within specified days
        /// Useful for notifications and alerts
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="withinDays">Number of days to check for expiration (default: 7)</param>
        /// <returns>True if acting period expires within specified days</returns>
        public static bool IsActingExpiringSoon(User user, int withinDays = 7)
        {
            if (user == null) return false;

            if (!IsCurrentlyActing(user) || !user.ActingEndDate.HasValue)
                return false;

            var daysRemaining = GetActingDaysRemaining(user);
            return daysRemaining <= withinDays && daysRemaining > 0;
        }
    }
}