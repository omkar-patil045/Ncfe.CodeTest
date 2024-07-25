using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncfe.CodeTest.src.Services.Interfaces
{
    // Interface for FailoverLearnerDataAccess
    public interface IFailoverLearnerDataAccess
    {
        LearnerResponse GetLearnerById(int learnerId);
    }
}
