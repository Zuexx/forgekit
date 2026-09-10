using Anvil.Extensions;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ForgeKit.Api.Tests.Extensions;

public sealed class SqlitePragmaInterceptorTests
{
    [Fact]
    public async Task ApplySqlitePragmas_SetsWalJournalModeAndBusyTimeout()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();
            DatabaseProviderExtensions.ApplySqlitePragmas(connection);

            await using var journalCmd = connection.CreateCommand();
            journalCmd.CommandText = "PRAGMA journal_mode;";
            var journalMode = (string)(await journalCmd.ExecuteScalarAsync())!;
            journalMode.ShouldBe("wal", StringCompareShould.IgnoreCase);

            await using var timeoutCmd = connection.CreateCommand();
            timeoutCmd.CommandText = "PRAGMA busy_timeout;";
            var busyTimeout = Convert.ToInt32(await timeoutCmd.ExecuteScalarAsync());
            busyTimeout.ShouldBeGreaterThan(0);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var f = dbPath + suffix;
                if (File.Exists(f)) File.Delete(f);
            }
        }
    }
}
