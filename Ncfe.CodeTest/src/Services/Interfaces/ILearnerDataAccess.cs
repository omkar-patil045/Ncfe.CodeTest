using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncfe.CodeTest.src.Services.Interfaces
{
    // Interface for LearnerDataAccess
    public interface ILearnerDataAccess
    {
        LearnerResponse LoadLearner(int learnerId);
    }
}
