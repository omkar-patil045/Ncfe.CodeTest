using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Ncfe.CodeTest;
using Ncfe.CodeTest.src.Services.Interfaces;

namespace Ncfe.CodeTest.Tests
{
    [TestFixture]
    public class LearnerServiceTests
    {
        private Mock<IArchivedDataService> archivedDataServiceMock;
        private Mock<ILearnerDataAccess> learnerDataAccessMock;
        private Mock<IFailoverLearnerDataAccess> failoverLearnerDataAccessMock;
        private Mock<IFailoverRepository> failoverRepositoryMock;
        private Mock<IConfigurationManager> configurationManagerMock;
        private Mock<IAppSettings> appSettingsMock;
        private LearnerService learnerService;

        [SetUp]
        public void SetUp()
        {
            archivedDataServiceMock = new Mock<IArchivedDataService>();
            learnerDataAccessMock = new Mock<ILearnerDataAccess>();
            failoverLearnerDataAccessMock = new Mock<IFailoverLearnerDataAccess>();
            failoverRepositoryMock = new Mock<IFailoverRepository>();
            configurationManagerMock = new Mock<IConfigurationManager>();
            appSettingsMock = new Mock<IAppSettings>();

            // Setting up the AppSettings for configurationManagerMock
            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("false");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            learnerService = new LearnerService(
                archivedDataServiceMock.Object,
                learnerDataAccessMock.Object,
                failoverLearnerDataAccessMock.Object,
                failoverRepositoryMock.Object,
                configurationManagerMock.Object
            );
        }

        [Test]
        public void GetLearner_WhenLearnerIsArchived_ReturnsArchivedLearner()
        {
            // Arrange: Setup the mock to return an archived learner when GetArchivedLearner is called
            int learnerId = 1;
            bool isLearnerArchived = true;
            var archivedLearner = new Learner { Id = learnerId, Name = "Archived Learner" };
            archivedDataServiceMock.Setup(x => x.GetArchivedLearner(learnerId)).Returns(archivedLearner);

            // Act: Call the GetLearner method with isLearnerArchived = true
            var result = learnerService.GetLearner(learnerId, isLearnerArchived);

            // Assert: Verify that the returned learner is the archived learner
            Assert.IsNotNull(result);
            Assert.AreEqual(archivedLearner.Id, result.Id);
            Assert.AreEqual(archivedLearner.Name, result.Name);
        }

        [Test]
        public void GetLearner_WhenInFailoverMode_ReturnsFailoverLearner()
        {
            // Arrange: Setup the mock to simulate failover mode and return a learner from the failover store
            int learnerId = 1;
            bool isLearnerArchived = false;
            var failoverLearner = new Learner { Id = learnerId, Name = "Failover Learner" };
            var learnerResponse = new LearnerResponse { IsArchived = false, Learner = failoverLearner };

            failoverLearnerDataAccessMock.Setup(x => x.GetLearnerById(learnerId)).Returns(learnerResponse);
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(new List<FailoverEntry> { new FailoverEntry { DateTime = DateTime.Now } });

            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("true");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act: Call the GetLearner method
            var result = learnerService.GetLearner(learnerId, isLearnerArchived);

            // Assert: Verify that the returned learner is from the failover store
            Assert.IsNotNull(result);
            Assert.AreEqual(failoverLearner.Id, result.Id);
            Assert.AreEqual(failoverLearner.Name, result.Name);
        }

        [Test]
        public void GetLearner_WhenNotInFailoverMode_ReturnsMainStoreLearner()
        {
            // Arrange: Setup the mock to simulate normal mode and return a learner from the main store
            int learnerId = 1;
            bool isLearnerArchived = false;
            var mainStoreLearner = new Learner { Id = learnerId, Name = "Main Store Learner" };
            var learnerResponse = new LearnerResponse { IsArchived = false, Learner = mainStoreLearner };
            learnerDataAccessMock.Setup(x => x.LoadLearner(learnerId)).Returns(learnerResponse);
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(new List<FailoverEntry>());

            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("false");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act: Call the GetLearner method
            var result = learnerService.GetLearner(learnerId, isLearnerArchived);

            // Assert: Verify that the returned learner is from the main store
            Assert.IsNotNull(result);
            Assert.AreEqual(mainStoreLearner.Id, result.Id);
            Assert.AreEqual(mainStoreLearner.Name, result.Name);
        }

        [Test]
        public void IsFailoverMode_WhenFailoverModeIsDisabled_ReturnsFalse()
        {
            // Arrange: Setup the mock to simulate failover mode being disabled
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(new List<FailoverEntry>());

            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("false");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act: Call the IsFailoverMode method using reflection
            var isFailoverModeMethod = learnerService.GetType().GetMethod("IsFailoverMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)isFailoverModeMethod.Invoke(learnerService, null);

            // Assert: Verify that the method returns false
            Assert.IsFalse(result);
        }

        [Test]
        public void IsFailoverMode_WhenFailoverModeIsEnabledAndFailoverThresholdExceeded_ReturnsTrue()
        {
            // Arrange: Setup the mock to simulate failover mode being enabled and failover threshold exceeded
            var failoverEntries = new List<FailoverEntry>();
            for (int i = 0; i < 101; i++)
            {
                failoverEntries.Add(new FailoverEntry { DateTime = DateTime.Now });
            }
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(failoverEntries);

            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("true");
            appSettingsMock.Setup(x => x["FailoverThreshold"]).Returns("100");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act: Call the IsFailoverMode method using reflection
            var isFailoverModeMethod = learnerService.GetType().GetMethod("IsFailoverMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)isFailoverModeMethod.Invoke(learnerService, null);

            // Assert: Verify that the method returns true
            Assert.IsTrue(result);
        }

        [Test]
        public void GetLearner_WhenLearnerIsArchivedInFailoverMode_ReturnsArchivedLearner()
        {
            // Arrange: Setup the mock to simulate failover mode and return an archived learner from the failover store
            int learnerId = 1;
            bool isLearnerArchived = false;
            var archivedFailoverLearner = new Learner { Id = learnerId, Name = "Archived Failover Learner" };
            var learnerResponse = new LearnerResponse { IsArchived = true, Learner = archivedFailoverLearner };

            failoverLearnerDataAccessMock.Setup(x => x.GetLearnerById(learnerId)).Returns(learnerResponse);
            archivedDataServiceMock.Setup(x => x.GetArchivedLearner(learnerId)).Returns(archivedFailoverLearner);
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(new List<FailoverEntry> { new FailoverEntry { DateTime = DateTime.Now } });
            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("true");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act: Call the GetLearner method
            var result = learnerService.GetLearner(learnerId, isLearnerArchived);

            // Assert: Verify that the returned learner is the archived learner from the failover store
            Assert.IsNotNull(result, "Expected a learner but got null.");
            Assert.AreEqual(archivedFailoverLearner.Id, result.Id, "Learner ID does not match.");
            Assert.AreEqual(archivedFailoverLearner.Name, result.Name, "Learner name does not match.");
        }

        [Test]
        public void GetLearner_WhenLearnerNotFound_ThrowsApplicationException()
        {
            // Arrange: Setup the mock to return null for learner not found scenario
            int learnerId = 1;
            bool isLearnerArchived = false;

            failoverLearnerDataAccessMock.Setup(x => x.GetLearnerById(learnerId)).Returns((LearnerResponse)null);
            learnerDataAccessMock.Setup(x => x.LoadLearner(learnerId)).Returns((LearnerResponse)null);
            archivedDataServiceMock.Setup(x => x.GetArchivedLearner(learnerId)).Returns((Learner)null);
            failoverRepositoryMock.Setup(x => x.GetFailOverEntries()).Returns(new List<FailoverEntry> { new FailoverEntry { DateTime = DateTime.Now } });
            appSettingsMock.Setup(x => x["IsFailoverModeEnabled"]).Returns("true");
            configurationManagerMock.Setup(cm => cm.AppSettings).Returns(appSettingsMock.Object);

            // Act & Assert: Call the GetLearner method and verify that an ApplicationException is thrown
            var ex = Assert.Throws<ApplicationException>(() => learnerService.GetLearner(learnerId, isLearnerArchived));
            Assert.That(ex.Message, Is.EqualTo($"An error occurred while retrieving the learner: Learner with ID {learnerId} not found."));
        }

        [Test]
        public void GetLearner_WhenLearnerIdIsInvalid_ThrowsArgumentException()
        {
            // Arrange: Setup invalid learner ID
            int invalidLearnerId = 0; // Invalid learner ID
            bool isLearnerArchived = false;

            // Act & Assert: Call the GetLearner method and verify that an ArgumentException is thrown
            var ex = Assert.Throws<ArgumentException>(() => learnerService.GetLearner(invalidLearnerId, isLearnerArchived));
            Assert.That(ex.Message, Is.EqualTo("Learner ID must be a positive integer."));
        }
    }
}
