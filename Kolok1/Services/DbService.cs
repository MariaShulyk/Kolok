using System.Data;
using System.Data.SqlClient;
using Kolok1.DTOs;

namespace Kolok1.Services;

public interface IDbService
{
    Task<GetGenresDTO?> GetGenresByIdAsync(int id);
    Task<GetGenresDTO>? AddBookAsync(AddBookDTO bookGenreDto);
}

public class DbService(IConfiguration configuration) : IDbService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<GetGenresDTO?> GetGenresByIdAsync(int id)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = """
                              SELECT books.PK, title, name FROM books g 
                              LEFT JOIN books_genres on books.PK = FK_book
                              LEFT JOIN genres on genres.PK = FK_genre
                              WHERE books.PK = @id
                              """;
        command.Parameters.AddWithValue("@id", id);
        var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return null;
        await reader.ReadAsync();
        
        var result = new GetGenresDTO(
            reader.GetInt32(0), 
            reader.GetString(1), 
            !await reader.IsDBNullAsync(2) ? [reader.GetString(2)] : []
        );
        while (await reader.ReadAsync())
        {
            result.Genres.Add(reader.GetString(2));
        }

        return result;
    }

    public async Task<GetGenresDTO> AddBookAsync(AddBookDTO bookGenreDto)
    {
        await using var connection = await GetConnection();
        
        var command = new SqlCommand();
        command.Connection = connection;
        foreach (var genreId in bookGenreDto.Genres)
        {
            command.Parameters.Clear();
            command.CommandText = """
                                   SELECT PK FROM genres
                                   WHERE PK = @genreId
                                   """;
            command.Parameters.AddWithValue("@genreId", genreId);
            
            var reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                await reader.DisposeAsync();
                throw new Exception($"Genre with id: {genreId} doesn't exist");
            }
            await reader.DisposeAsync();
        }
        
        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            
            
            var command1 = new SqlCommand();
            command1.Connection = connection;
            command1.CommandText = """
                                   INSERT INTO books VALUES(@title)
                                   select cast(scope_identity() as int)";
                                   """;
            command1.Parameters.AddWithValue("@title", bookGenreDto.Title);
            command1.Transaction = (SqlTransaction)transaction;
            
            var bookId = (int)(await command1.ExecuteScalarAsync())!;
            
            var command3 = new SqlCommand();
            command3.Connection = connection;
            command3.Transaction = (SqlTransaction)transaction;
            foreach (var genreId in bookGenreDto.Genres)
            {
                command3.Parameters.Clear();
                command3.CommandText = """
                                       INSERT INTO books_genres VALUES(@bookId,@genreId)";
                                       """;
                command3.Parameters.AddWithValue("@bookId", bookId);
                command3.Parameters.AddWithValue("@genreId", genreId);
                command3.Transaction = (SqlTransaction)transaction;
                var reader = await command3.ExecuteReaderAsync();
                await reader.DisposeAsync();
            }

            await transaction.CommitAsync();
            return await GetGenresByIdAsync(bookId);
            
        }catch(Exception){
            await transaction.RollbackAsync();
            throw;
        }
    }
}
