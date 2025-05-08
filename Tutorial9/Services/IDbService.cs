namespace Tutorial9.Services;
using Tutorial9.Model.DTOs;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task<int> AddProductToWarehouseAsync(WarehouseRequest request);

}