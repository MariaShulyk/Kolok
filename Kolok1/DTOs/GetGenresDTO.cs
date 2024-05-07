namespace Kolok1.DTOs;

public record GetGenresDTO(int Id, string Title, List<string> Genres);
public record AddBookDTO(string Title, List<int> Genres);