namespace GoalSettingApp.Services
{
    public class GoalService
    {
        private List<Goal> _goals = new List<Goal>();
        private int _nextId = 1;

        /// <summary>
        /// Adds a new goal to the collection
        /// </summary>
        /// <param name="title">The title of the goal</param>
        /// <param name="description">The description of the goal</param>
        /// <param name="category">The category of the goal</param>
        /// <param name="priority">The priority level of the goal</param>
        /// <returns>The newly created goal</returns>
        public Goal AddGoal(string title, string description, string category, PriorityLevel priority = PriorityLevel.Medium)
        {
            var goal = new Goal
            {
                Id = _nextId++,
                Title = title,
                Description = description,
                Category = category,
                Priority = priority,
                CreatedAt = DateTime.Now
            };

            _goals.Add(goal);
            return goal;
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
        public bool EditGoal(int id, string title, string description, string category, PriorityLevel priority)
        {
            var goal = _goals.FirstOrDefault(g => g.Id == id);
            
            if (goal == null)
            {
                return false;
            }

            goal.Title = title;
            goal.Description = description;
            goal.Category = category;
            goal.Priority = priority;

            return true;
        }

        /// <summary>
        /// Deletes a goal by its ID
        /// </summary>
        /// <param name="id">The ID of the goal to delete</param>
        /// <returns>True if the goal was found and deleted, false otherwise</returns>
        public bool DeleteGoal(int id)
        {
            var goal = _goals.FirstOrDefault(g => g.Id == id);
            
            if (goal == null)
            {
                return false;
            }

            _goals.Remove(goal);
            return true;
        }

        /// <summary>
        /// Gets all goals
        /// </summary>
        /// <returns>A list of all goals</returns>
        public List<Goal> GetAllGoals()
        {
            return _goals.ToList();
        }

        /// <summary>
        /// Gets a goal by its ID
        /// </summary>
        /// <param name="id">The ID of the goal to retrieve</param>
        /// <returns>The goal if found, null otherwise</returns>
        public Goal? GetGoalById(int id)
        {
            return _goals.FirstOrDefault(g => g.Id == id);
        }

        /// <summary>
        /// Gets goals by category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>A list of goals in the specified category</returns>
        public List<Goal> GetGoalsByCategory(string category)
        {
            return _goals.Where(g => g.Category == category).ToList();
        }

        /// <summary>
        /// Gets goals by priority level
        /// </summary>
        /// <param name="priority">The priority level to filter by</param>
        /// <returns>A list of goals with the specified priority</returns>
        public List<Goal> GetGoalsByPriority(PriorityLevel priority)
        {
            return _goals.Where(g => g.Priority == priority).ToList();
        }
    }
}

