namespace AgriConnect.Api.Dtos.Common;

public class ItemsResponseDto<T>
{
    public List<T> Items { get; set; } = [];
}

public class PagedResponseDto<T> : ItemsResponseDto<T>
{
    public int Page { get; set; }

    public int Size { get; set; }

    public int Total { get; set; }
}
