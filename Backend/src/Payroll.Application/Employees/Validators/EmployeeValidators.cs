using FluentValidation;
using Payroll.Application.Employees.DTOs;

namespace Payroll.Application.Employees.Validators;

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IdNumber).NotEmpty().Length(13).Matches(@"^\d{13}$").WithMessage("SA ID number must be 13 digits.");
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today).WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.StartDate).LessThanOrEqualTo(DateTime.Today.AddDays(30));
        RuleFor(x => x.BasicSalary).GreaterThan(0);
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BasicSalary).GreaterThan(0);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
