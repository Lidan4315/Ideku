using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.Services.Notification;

namespace Ideku.Services.IdeaRelation
{
    /// <summary>
    /// Service implementation for managing idea relations with divisions
    /// </summary>
    public class IdeaRelationService : IIdeaRelationService
    {
        private readonly ILookupRepository _lookupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIdeaRepository _ideaRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<IdeaRelationService> _logger;

        public IdeaRelationService(
            ILookupRepository lookupRepository,
            IUserRepository userRepository,
            IIdeaRepository ideaRepository,
            INotificationService notificationService,
            ILogger<IdeaRelationService> logger)
        {
            _lookupRepository = lookupRepository;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<Division>> GetAvailableDivisionsAsync(long ideaId)
        {
            try
            {
                _logger.LogInformation("Getting available divisions for idea {IdeaId}", ideaId);

                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    _logger.LogWarning("Idea {IdeaId} not found", ideaId);
                    return new List<Division>();
                }

                var allDivisions = await _lookupRepository.GetActiveDivisionsAsync();
                
                // Exclude target division (business rule: don't notify same division)
                var availableDivisions = allDivisions
                    .Where(d => d.Id != idea.ToDivisionId)
                    .OrderBy(d => d.NameDivision)
                    .ToList();

                _logger.LogInformation("Found {Count} available divisions for idea {IdeaId}", 
                    availableDivisions.Count, ideaId);

                return availableDivisions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available divisions for idea {IdeaId}", ideaId);
                throw;
            }
        }

        public async Task<List<User>> GetWorkstreamLeadersAsync(List<string> divisionIds)
        {
            try
            {
                if (!divisionIds?.Any() == true)
                {
                    _logger.LogInformation("No division IDs provided, returning empty list");
                    return new List<User>();
                }

                _logger.LogInformation("Getting workstream leaders for divisions: {DivisionIds}", 
                    string.Join(", ", divisionIds));

                var workstreamLeaders = await _userRepository.GetWorkstreamLeadersByDivisionsAsync(divisionIds);

                _logger.LogInformation("Found {Count} workstream leaders for divisions {DivisionIds}", 
                    workstreamLeaders.Count, string.Join(", ", divisionIds));

                return workstreamLeaders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workstream leaders for divisions {DivisionIds}", 
                    string.Join(", ", divisionIds ?? new List<string>()));
                throw;
            }
        }

        public async Task UpdateIdeaRelatedDivisionsAsync(long ideaId, List<string> divisionIds)
        {
            try
            {
                _logger.LogInformation("Updating related divisions for idea {IdeaId}: {DivisionIds}", 
                    ideaId, string.Join(", ", divisionIds ?? new List<string>()));

                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    throw new ArgumentException($"Idea with ID {ideaId} not found");
                }

                // Update using the clean property (auto-converts to JSON)
                idea.RelatedDivisions = divisionIds ?? new List<string>();
                await _ideaRepository.UpdateAsync(idea);

                _logger.LogInformation("Successfully updated related divisions for idea {IdeaId}", ideaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating related divisions for idea {IdeaId}", ideaId);
                throw;
            }
        }

        public async Task NotifyRelatedDivisionsAsync(Models.Entities.Idea idea, List<string> divisionIds)
        {
            try
            {
                if (!divisionIds?.Any() == true)
                {
                    _logger.LogInformation("No related divisions to notify for idea {IdeaId}", idea.Id);
                    return;
                }

                _logger.LogInformation("Sending notifications to related divisions for idea {IdeaId}: {DivisionIds}", 
                    idea.Id, string.Join(", ", divisionIds));

                var workstreamLeaders = await GetWorkstreamLeadersAsync(divisionIds);
                
                if (!workstreamLeaders.Any())
                {
                    _logger.LogWarning("No workstream leaders found for divisions {DivisionIds}", 
                        string.Join(", ", divisionIds));
                    return;
                }

                // Send notification to workstream leaders using bulk notification
                await _notificationService.NotifyWorkstreamLeadersAsync(idea, workstreamLeaders);

                _logger.LogInformation("Successfully sent {Count} notifications for idea {IdeaId}", 
                    workstreamLeaders.Count, idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying related divisions for idea {IdeaId}", idea.Id);
                throw;
            }
        }

        public async Task<List<Division>> GetRelatedDivisionsAsync(long ideaId)
        {
            try
            {
                _logger.LogInformation("Getting related divisions for idea {IdeaId}", ideaId);

                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null || !idea.RelatedDivisions.Any())
                {
                    return new List<Division>();
                }

                var allDivisions = await _lookupRepository.GetActiveDivisionsAsync();
                var relatedDivisions = allDivisions
                    .Where(d => idea.RelatedDivisions.Contains(d.Id))
                    .OrderBy(d => d.NameDivision)
                    .ToList();

                _logger.LogInformation("Found {Count} related divisions for idea {IdeaId}", 
                    relatedDivisions.Count, ideaId);

                return relatedDivisions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related divisions for idea {IdeaId}", ideaId);
                throw;
            }
        }
    }
}