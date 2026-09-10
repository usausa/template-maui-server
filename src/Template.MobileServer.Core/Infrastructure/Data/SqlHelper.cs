namespace Template.MobileServer.Infrastructure.Data;

// SQL文字列の組み立てに関する補助。Smart.Data.Accessorの生SQL出力(/*# */)へ渡す値を作る
public static class SqlHelper
{
    // ORDER BY句はSQLへそのまま展開されるため、呼び出し側の文字列を素通しさせない。
    // どの列を許可するか・一致しなかったときにどれを使うかはテーブルごとの都合なので、いずれも呼び出し側が指定する
    public static string NormalizeSort(string[] allowed, string defaultColumn, string? sort, bool desc)
    {
        ArgumentNullException.ThrowIfNull(allowed);

        var column = Array.IndexOf(allowed, sort) >= 0 ? sort! : defaultColumn;
        return desc ? $"{column} DESC" : column;
    }
}
