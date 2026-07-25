namespace tenon_example.Tests;

/// <summary>每个测试用独立 SQLite 文件,跑完删掉,别在临时目录里堆积。</summary>
internal static class TestDb
{
    public static void Cleanup(string sqliteFile)
    {
        try { if (File.Exists(sqliteFile)) File.Delete(sqliteFile); } catch { /* 尽力而为 */ }
    }
}
