using FluentValidation.Results;

namespace Application.Exceptions;

public class ValidationException : Exception
{
    public List<ValidationFailure> Failures { get; init; } = [];
    public ValidationException(string? message, List<ValidationFailure> failures) : base(message){}
    public ValidationException(string? message, Exception? innerException, List<ValidationFailure> failures)
        : base(message, innerException){}
}