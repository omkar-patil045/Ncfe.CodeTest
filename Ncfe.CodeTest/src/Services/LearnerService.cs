using Ncfe.CodeTest.src.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncfe.CodeTest
{
    public class LearnerService
    {
        private readonly IArchivedDataService archivedDataService;
        private readonly ILearnerDataAccess learnerDataAccess;
        private readonly IFailoverLearnerDataAccess failoverLearnerDataAccess;
        private readonly IFailoverRepository failoverRepository;
        private readonly IConfigurationManager configurationManager;

        // Constructor with all dependencies injected
        public LearnerService(
            IArchivedDataService archivedDataService,
            ILearnerDataAccess learnerDataAccess,
            IFailoverLearnerDataAccess failoverLearnerDataAccess,
            IFailoverRepository failoverRepository,
            IConfigurationManager configurationManager)
            {
            this.archivedDataService = archivedDataService;
            this.learnerDataAccess = learnerDataAccess;
            this.failoverLearnerDataAccess = failoverLearnerDataAccess;
            this.failoverRepository = failoverRepository;
            this.configurationManager = configurationManager;
        }

        // Method to get the learner, with dependencies injected
        public Learner GetLearner(int learnerId, bool isLearnerArchived)
        {
            try
            {
                if (learnerId <= 0)
                {
                    throw new ArgumentException("Learner ID must be a positive integer.");
                }

                Learner learner = null;

                // If the learner is archived, retrieve from the archived data service
                if (isLearnerArchived)
                {
                    learner = archivedDataService.GetArchivedLearner(learnerId);
                    if (learner == null)
                    {
                        throw new Exception($"Archived learner with ID {learnerId} not found.");
                    }
                    return learner;
                }

                // Check if the system is in failover mode
                if (IsFailoverMode())
                {
                    var learnerResponse = failoverLearnerDataAccess.GetLearnerById(learnerId);
                    if (learnerResponse == null)
                    {
                        throw new Exception($"Learner with ID {learnerId} not found in failover mode.");
                    }
                    learner = learnerResponse.IsArchived ? archivedDataService.GetArchivedLearner(learnerId) : learnerResponse.Learner;
                }
                else
                {
                    var learnerResponse = learnerDataAccess.LoadLearner(learnerId);
                    if (learnerResponse == null)
                    {
                        throw new Exception($"Learner with ID {learnerId} not found.");
                    }
                    learner = learnerResponse.IsArchived ? archivedDataService.GetArchivedLearner(learnerId) : learnerResponse.Learner;
                }

                if (learner == null)
                {
                    throw new Exception($"Learner with ID {learnerId} could not be retrieved.");
                }

                return learner;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                throw new ApplicationException($"An error occurred while retrieving the learner: {ex.Message}", ex);
            }
        }

        // Private method to check if failover mode should be enabled
        private bool IsFailoverMode()
        {
            try
            {
                // Retrieve failover entries and count failed requests within the last 10 minutes
                var failoverEntries = failoverRepository.GetFailOverEntries();
                if (failoverEntries == null)
                {
                    throw new Exception("Failover entries data is null.");
                }

                var failedRequests = failoverEntries.Count(entry => entry.DateTime > DateTime.Now.AddMinutes(-10));

                // Check if failover mode is enabled in the configuration
                var isFailoverEnabled = configurationManager.AppSettings["IsFailoverModeEnabled"].Equals("true", StringComparison.OrdinalIgnoreCase);
                return failedRequests > Convert.ToInt32(configurationManager.AppSettings["FailoverThreshold"]) && isFailoverEnabled;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                throw new ApplicationException($"An error occurred while checking the failover mode: {ex.Message}", ex);
            }
        }
    }
}