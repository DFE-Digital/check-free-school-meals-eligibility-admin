using CheckYourEligibility.Admin.Models;

namespace CheckYourEligibility.Admin.UseCases;

public interface IProcessChildDetailsUseCase
{
    Task<FsmApplication> Execute(Children children);
}

public class ProcessChildDetailsUseCase : IProcessChildDetailsUseCase
{
    public Task<FsmApplication> Execute(Children children)
    {
        var fsmApplication = new FsmApplication
        {
            ParentFirstName = children.ParentFirstName,
            ParentLastName = children.ParentLastName,
            ParentDateOfBirth = children.ParentDateOfBirth,
            ParentNass = children.ParentNass,
            ParentNino = children.ParentNino,
            ParentEmail = children.ParentEmail,
            Tier = children.Tier,
            EligibilityEndDate = children.EligibilityEndDate,
            Children = children
        };

        return Task.FromResult(fsmApplication);
    }
}