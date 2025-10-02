namespace Core.Interfaces;

public interface IUseCase<TResponse, in TInput>
{
    public Task<TResponse> Handle(TInput input, CancellationToken cancellationToken = default);
}