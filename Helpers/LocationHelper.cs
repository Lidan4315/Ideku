using Ideku.Models.Entities;

namespace Ideku.Helpers
{
    /// <summary>
    /// Helper class for User location calculations and display
    /// Handles original vs acting location logic for divisions and departments
    /// </summary>
    public static class LocationHelper
    {
        /// <summary>
        /// Get original division name from Employee
        /// Returns the user's permanent division from their employee record
        /// </summary>
        /// <param name="user">User to get division for</param>
        /// <returns>Original division name or "N/A" if not available</returns>
        public static string GetOriginalDivision(User user)
        {
            if (user?.Employee?.DivisionNavigation == null) return "N/A";
            return user.Employee.DivisionNavigation.NameDivision ?? "N/A";
        }

        /// <summary>
        /// Get original department name from Employee
        /// Returns the user's permanent department from their employee record
        /// </summary>
        /// <param name="user">User to get department for</param>
        /// <returns>Original department name or "N/A" if not available</returns>
        public static string GetOriginalDepartment(User user)
        {
            if (user?.Employee?.DepartmentNavigation == null) return "N/A";
            return user.Employee.DepartmentNavigation.NameDepartment ?? "N/A";
        }

        /// <summary>
        /// Get effective division (acting override or original)
        /// Returns acting division if user is currently acting, otherwise original division
        /// </summary>
        /// <param name="user">User to get effective division for</param>
        /// <returns>Effective division name</returns>
        public static string GetEffectiveDivision(User user)
        {
            if (user == null) return "N/A";

            if (ActingHelper.IsCurrentlyActing(user) && !string.IsNullOrEmpty(user.ActingDivisionId))
            {
                return user.ActingDivision?.NameDivision ?? GetOriginalDivision(user);
            }

            return GetOriginalDivision(user);
        }

        /// <summary>
        /// Get effective department (acting override or original)
        /// Returns acting department if user is currently acting, otherwise original department
        /// </summary>
        /// <param name="user">User to get effective department for</param>
        /// <returns>Effective department name</returns>
        public static string GetEffectiveDepartment(User user)
        {
            if (user == null) return "N/A";

            if (ActingHelper.IsCurrentlyActing(user) && !string.IsNullOrEmpty(user.ActingDepartmentId))
            {
                return user.ActingDepartment?.NameDepartment ?? GetOriginalDepartment(user);
            }

            return GetOriginalDepartment(user);
        }

        /// <summary>
        /// Check if user has acting location override
        /// Determines if user has specified different acting division or department
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>True if user has acting location override</returns>
        public static bool HasActingLocationOverride(User user)
        {
            if (user == null) return false;
            return !string.IsNullOrEmpty(user.ActingDivisionId) || !string.IsNullOrEmpty(user.ActingDepartmentId);
        }

        /// <summary>
        /// Get complete location display text with acting indicator
        /// Shows current location with indication if it's different from original
        /// </summary>
        /// <param name="user">User to get location display for</param>
        /// <returns>Formatted location text with acting indicator if applicable</returns>
        public static string GetLocationDisplayText(User user)
        {
            if (user == null) return "N/A";

            if (ActingHelper.IsCurrentlyActing(user) && HasActingLocationOverride(user))
            {
                var effectiveDiv = GetEffectiveDivision(user);
                var effectiveDept = GetEffectiveDepartment(user);
                var originalDiv = GetOriginalDivision(user);
                var originalDept = GetOriginalDepartment(user);

                if (effectiveDiv != originalDiv || effectiveDept != originalDept)
                {
                    return $"{effectiveDiv} - {effectiveDept} (Acting from {originalDiv} - {originalDept})";
                }
            }

            return $"{GetOriginalDivision(user)} - {GetOriginalDepartment(user)}";
        }

        /// <summary>
        /// Get effective division ID (acting override or original) for approval workflow
        /// Returns the division ID that should be used for workflow and approval logic
        /// </summary>
        /// <param name="user">User to get effective division ID for</param>
        /// <returns>Effective division ID for workflow processing</returns>
        public static string GetEffectiveDivisionId(User user)
        {
            if (user == null) return string.Empty;

            if (ActingHelper.IsCurrentlyActing(user) && !string.IsNullOrEmpty(user.ActingDivisionId))
            {
                return user.ActingDivisionId;
            }

            return user.Employee?.DIVISION ?? string.Empty;
        }

        /// <summary>
        /// Get effective department ID (acting override or original) for approval workflow
        /// Returns the department ID that should be used for workflow and approval logic
        /// </summary>
        /// <param name="user">User to get effective department ID for</param>
        /// <returns>Effective department ID for workflow processing</returns>
        public static string GetEffectiveDepartmentId(User user)
        {
            if (user == null) return string.Empty;

            if (ActingHelper.IsCurrentlyActing(user) && !string.IsNullOrEmpty(user.ActingDepartmentId))
            {
                return user.ActingDepartmentId;
            }

            return user.Employee?.DEPARTEMENT ?? string.Empty;
        }
    }
}