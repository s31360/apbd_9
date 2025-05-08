using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string not found.");
    }

    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand
        {
            Connection = connection
        };

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");

            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");

            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand
        {
            Connection = connection,
            CommandText = "NazwaProcedury",
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();

        command.Parameters.AddWithValue("@Id", 2);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<int> AddProductToWarehouseAsync(WarehouseRequest request)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    await using var command = new SqlCommand();
    command.Connection = connection;
    command.Transaction = (SqlTransaction)await connection.BeginTransactionAsync();

    try
    {
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        var productExists = await command.ExecuteScalarAsync();
        if (productExists == null)
            throw new ArgumentException("Product does not exist");

        command.Parameters.Clear();

        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        var warehouseExists = await command.ExecuteScalarAsync();
        if (warehouseExists == null)
            throw new ArgumentException("Warehouse does not exist");

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        command.Parameters.Clear();

        command.CommandText = @"
            SELECT IdOrder FROM [Order]
            WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        var idOrderObj = await command.ExecuteScalarAsync();
        if (idOrderObj == null)
            throw new InvalidOperationException("No matching order found");

        int idOrder = (int)idOrderObj;
        command.Parameters.Clear();

        command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        var alreadyCompleted = await command.ExecuteScalarAsync();
        if (alreadyCompleted != null)
            throw new InvalidOperationException("Order already completed");

        command.Parameters.Clear();

        command.CommandText = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        await command.ExecuteNonQueryAsync();

        command.Parameters.Clear();

        command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        var priceObj = await command.ExecuteScalarAsync();
        decimal price = Convert.ToDecimal(priceObj);

        decimal totalPrice = price * request.Amount;

        command.Parameters.Clear();

        command.CommandText = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            OUTPUT INSERTED.IdProductWarehouse
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE())";

        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@Price", totalPrice);

        var insertedId = (int)await command.ExecuteScalarAsync();

        await command.Transaction.CommitAsync();
        return insertedId;
    }
    catch
    {
        await command.Transaction.RollbackAsync();
        throw;
    }
    
}
    public async Task<int> AddProductWithProcedureAsync(WarehouseRequest request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("AddProductToWarehouse", connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result); 
    }

}
