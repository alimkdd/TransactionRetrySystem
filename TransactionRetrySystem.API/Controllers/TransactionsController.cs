using Microsoft.AspNetCore.Mvc;
using TransactionRetrySystem.Application.Dtos.Responses;
using TransactionRetrySystem.Application.Interfaces;

namespace TransactionRetrySystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController : ControllerBase
{
    ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    #region Get

    [HttpGet("{id}/retry-history")]
    public async Task<List<RetryHistoryResponse>> GetRetryHistory(int id, CancellationToken token)
        => await _transactionService.GetRetryHistory(id, token);


    [HttpGet("{id}/status")]
    public async Task<string> GetTransactionStatus(int id, CancellationToken token)
        => await _transactionService.GetTransactionStatus(id, token);

    #endregion

    #region POST

    [HttpPost("{id}/retry")]
    public async Task<string> RetryTransaction(int id, CancellationToken token)
        => await _transactionService.RetryTransaction(id, token);


    [HttpPost("{id}/cancel")]
    public async Task<string> CancelTransactionRetries(int id, CancellationToken token)
        => await _transactionService.CancelTransactionRetries(id, token);

    #endregion
}