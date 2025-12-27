using Mapster;
using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Domain.Models;

namespace TransactionRetrySystem.Application.Mapper;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RetryQueue, RetryHistoryResponse>()
            .Map(dest => dest.TransactionId, src => src.TransactionId)
            .Map(dest => dest.Status, src => src.Status.Name)
            .Map(dest => dest.ScheduledRetryTime, src => src.ScheduledRetryTime)
            .Map(dest => dest.RetryCount, src => src.RetryCount);
    }
}