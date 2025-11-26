using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GoalSettingApp.Services
{
    public class GoalService
    {
        private readonly Supabase.Client _supabase;
        private readonly AuthenticationStateProvider _authStateProvider;

        public GoalService(Supabase.Client supabase, AuthenticationStateProvider authStateProvider)
        {
            _supabase = supabase;
            _authStateProvider = authStateProvider;
        }

        /// <summary>
        /// Gets the current user's ID from the authentication state
        /// </summary>
        private async Task<string?> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Adds a new goal to the database
        /// </summary>
        /// <param name="title">The title of the goal</param>
        /// <param name="description">The description of the goal</param>
        /// <param name="category">The category of the goal</param>
        /// <param name="priority">The priority level of the goal</param>
        /// <returns>The newly created goal</returns>
        public async Task<Goal?> AddGoalAsync(string title, string description, string category, PriorityLevel priority, DateTime? dueDate)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var goal = new Goal
            {
                UserId = userId,
                Title = title,
                Description = description,
                Category = category,
                Priority = priority,
                DueDate = dueDate,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            var response = await _supabase
                .From<Goal>()
                .Insert(goal);

            return response.Models.FirstOrDefault();
        }

        /// <summary>
        /// Edits an existing goal
        /// </summary>
        /// <param name="id">The ID of the goal to edit</param>
        /// <param name="title">The new title</param>
        /// <param name="description">The new description</param>
        /// <param name="category">The new category</param>
        /// <param name="priority">The new priority level</param>
        /// <returns>True if the goal was found and updated, false otherwise</returns>
        public async Task<bool> EditGoalAsync(int id, string title, string description, string category, PriorityLevel priority, DateTime? dueDate)
        {
            var userId = await GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var goalExists = await GetGoalByIdAsync(id);

            if (goalExists == null)
            {
                return false;
            }

            var goal = new Goal
            {
                Id = id,
                UserId = userId,
                Title = title,
                Description = description,
                Category = category,
                Priority = priority,
                DueDate = dueDate ?? goalExists.DueDate,
                CreatedAt = goalExists.CreatedAt,
            };

            await _supabase
                .From<Goal>()
                .Where(g => g.Id == id && g.UserId == userId)
                .Update(goal);

            return true;
        }

        /// <summary>
        /// Deletes a goal by its ID
        /// </summary>
        /// <param name="id">The ID of the goal to delete</param>
        /// <returns>True if the goal was found and deleted, false otherwise</returns>
        public async Task<bool> DeleteGoalAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            await _supabase
                .From<Goal>()
                .Where(g => g.Id == id && g.UserId == userId)
                .Delete();

            return true;
        }

        /// <summary>
        /// Gets all goals for the current user
        /// </summary>
        /// <returns>A list of all goals</returns>
        public async Task<List<Goal>> GetAllGoalsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return new List<Goal>();
            }

            var response = await _supabase
                .From<Goal>()
                .Where(g => g.UserId == userId)
                .Get();

            return response.Models;
        }

        /// <summary>
        /// Gets a goal by its ID
        /// </summary>
        /// <param name="id">The ID of the goal to retrieve</param>
        /// <returns>The goal if found, null otherwise</returns>
        public async Task<Goal?> GetGoalByIdAsync(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var response = await _supabase
                .From<Goal>()
                .Where(g => g.Id == id && g.UserId == userId)
                .Get();

            return response.Models.FirstOrDefault();
        }

        /// <summary>
        /// Gets goals by category for the curret user
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>A list of goals in the specified category</returns>
        public async Task<List<Goal>> GetGoalsByCategoryAsync(string category)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return new List<Goal>();
            }

            var response = await _supabase
                .From<Goal>()
                .Where(g => g.UserId == userId && g.Category == category)
                .Get();

            return response.Models;
        }

        /// <summary>
        /// Gets goals by priority level for the current user
        /// </summary>
        /// <param name="priority">The priority level to filter by</param>
        /// <returns>A list of goals with the specified priority</returns>
        public async Task<List<Goal>> GetGoalsByPriorityAsync(PriorityLevel priority)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return new List<Goal>();
            }

            var response = await _supabase
                .From<Goal>()
                .Where(g => g.UserId == userId && g.PriorityString == priority.ToString())
                .Get();

            return response.Models;
        }

        public async Task<bool> ToggleGoalCompletedAsync(int id, bool isCompleted)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var existingGoal = await GetGoalByIdAsync(id);
            if (existingGoal == null)
            {
                return false;
            }

            existingGoal.IsCompleted = isCompleted;

            await _supabase
                .From<Goal>()
                .Where(g => g.Id == id && g.UserId == userId)
                .Update(existingGoal);

            return true;
        }
    }
}

