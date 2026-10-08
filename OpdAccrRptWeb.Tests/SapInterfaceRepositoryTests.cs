using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class SapInterfaceRepositoryTests
{
    public static TheoryData<string, int[], SapInterfaceRepositoryRunStatus> InsertRowCountExamples => new()
    {
        { "SAPCASH", [0], SapInterfaceRepositoryRunStatus.NoData },
        { "SAPACC", [0, 0], SapInterfaceRepositoryRunStatus.NoData },
        { "SAPACC", [0, 3], SapInterfaceRepositoryRunStatus.Completed },
        { "SAPREV2", [0, 0, 0], SapInterfaceRepositoryRunStatus.NoData },
        { "SAPREV2", [0, 2, 0], SapInterfaceRepositoryRunStatus.Completed }
    };

    [Fact]
    public void GetEventStatements_SeparatesTargetDeleteFromInsertCommands()
    {
        Assert.Single(SapInterfaceRepository.GetEventStatements("SAPCASH").InsertSql);
        Assert.Single(SapInterfaceRepository.GetEventStatements("SAPCONS").InsertSql);
        Assert.Equal(2, SapInterfaceRepository.GetEventStatements("SAPACC").InsertSql.Length);
        Assert.Equal(3, SapInterfaceRepository.GetEventStatements("SAPREV2").InsertSql.Length);
        Assert.All(new[] { "SAPCASH", "SAPCONS", "SAPACC", "SAPREV2" }, eventCode =>
            Assert.DoesNotContain(SapInterfaceRepository.GetEventStatements(eventCode).DeleteSql,
                SapInterfaceRepository.GetEventStatements(eventCode).InsertSql));
    }

    [Fact]
    public void SumInsertedRowCounts_SumsOnlyAffectedInsertCountsAcrossEventStatements()
    {
        Assert.Equal(0, SapInterfaceRepository.SumInsertedRowCounts([0]));
        Assert.Equal(0, SapInterfaceRepository.SumInsertedRowCounts([0, 0]));
        Assert.Equal(0, SapInterfaceRepository.SumInsertedRowCounts([0, 0, 0]));
        Assert.Equal(3, SapInterfaceRepository.SumInsertedRowCounts([0, 3]));
        Assert.Equal(2, SapInterfaceRepository.SumInsertedRowCounts([0, 2, 0]));
    }

    [Fact]
    public void SumInsertedRowCounts_RejectsUnknownAffectedRowCount()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SapInterfaceRepository.SumInsertedRowCounts([0, -1]));
    }

    [Fact]
    public async Task EmptyFirstRun_RollsBackWithoutCreatingCompletionRecord()
    {
        var state = new FakeTransactionState([], isCompleted: false);
        state.DeletePriorData();

        SapInterfaceRepositoryRunResult result = await RunEmptyEventAsync(state);

        Assert.Equal(SapInterfaceRepositoryRunStatus.NoData, result.Status);
        Assert.Empty(state.TargetRows);
        Assert.False(state.IsCompleted);
        Assert.Equal(0, state.LogWrites);
        Assert.Equal(0, state.Commits);
        Assert.Equal(1, state.Rollbacks);
    }

    [Fact]
    public async Task EmptyConfirmedRerun_RollsBackAndPreservesPreviousRowsAndCompletion()
    {
        var state = new FakeTransactionState(["prior-row"], isCompleted: true);
        state.DeletePriorData();

        SapInterfaceRepositoryRunResult result = await RunEmptyEventAsync(state);

        Assert.Equal(SapInterfaceRepositoryRunStatus.NoData, result.Status);
        Assert.Equal(new[] { "prior-row" }, state.TargetRows);
        Assert.True(state.IsCompleted);
        Assert.Equal(0, state.LogWrites);
        Assert.Equal(0, state.Commits);
        Assert.Equal(1, state.Rollbacks);
    }

    [Fact]
    public async Task PositiveInsertTotal_WritesCompletionAndCommits()
    {
        int logWrites = 0;
        int commits = 0;
        int rollbacks = 0;
        Func<CancellationToken, Task<int>> noRows = _ => Task.FromResult(0);
        Func<CancellationToken, Task<int>> threeRows = _ => Task.FromResult(3);

        SapInterfaceRepositoryRunResult result = await SapInterfaceEventTransaction.ExecuteAsync(
            [noRows, threeRows],
            _ =>
            {
                logWrites++;
                return Task.CompletedTask;
            },
            _ =>
            {
                commits++;
                return Task.CompletedTask;
            },
            _ =>
            {
                rollbacks++;
                return Task.CompletedTask;
            }, CancellationToken.None);

        Assert.Equal(SapInterfaceRepositoryRunStatus.Completed, result.Status);
        Assert.Equal(3, result.InsertedRows);
        Assert.Equal(1, logWrites);
        Assert.Equal(1, commits);
        Assert.Equal(0, rollbacks);
    }

    [Theory]
    [MemberData(nameof(InsertRowCountExamples))]
    public async Task MultiStatementInsertTotals_MatchSpecExamples(string eventCode,
        int[] insertRowCounts, SapInterfaceRepositoryRunStatus expectedStatus)
    {
        SapInterfaceRepository.EventStatements statements = SapInterfaceRepository.GetEventStatements(eventCode);
        Assert.Equal(insertRowCounts.Length, statements.InsertSql.Length);

        var insertCommands = insertRowCounts
            .Select(count => (Func<CancellationToken, Task<int>>)(_ => Task.FromResult(count)))
            .ToArray();
        int logWrites = 0;
        int commits = 0;
        int rollbacks = 0;
        SapInterfaceRepositoryRunResult result = await SapInterfaceEventTransaction.ExecuteAsync(
            insertCommands,
            _ => { logWrites++; return Task.CompletedTask; },
            _ => { commits++; return Task.CompletedTask; },
            _ => { rollbacks++; return Task.CompletedTask; },
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(insertRowCounts.Sum(), result.InsertedRows);
        Assert.Equal(expectedStatus == SapInterfaceRepositoryRunStatus.Completed ? 1 : 0, logWrites);
        Assert.Equal(expectedStatus == SapInterfaceRepositoryRunStatus.Completed ? 1 : 0, commits);
        Assert.Equal(expectedStatus == SapInterfaceRepositoryRunStatus.NoData ? 1 : 0, rollbacks);
    }

    private static Task<SapInterfaceRepositoryRunResult> RunEmptyEventAsync(FakeTransactionState state)
    {
        Func<CancellationToken, Task<int>> emptyInsert = _ => Task.FromResult(0);
        return SapInterfaceEventTransaction.ExecuteAsync([emptyInsert, emptyInsert],
            _ =>
            {
                state.LogWrites++;
                return Task.CompletedTask;
            },
            _ =>
            {
                state.Commits++;
                return Task.CompletedTask;
            },
            state.RollbackAsync, CancellationToken.None);
    }

    private sealed class FakeTransactionState(IReadOnlyList<string> targetRows, bool isCompleted)
    {
        private string[] _priorRows = [];
        private bool _priorCompletion;

        public List<string> TargetRows { get; } = targetRows.ToList();
        public bool IsCompleted { get; private set; } = isCompleted;
        public int LogWrites { get; set; }
        public int Commits { get; set; }
        public int Rollbacks { get; private set; }

        public void DeletePriorData()
        {
            _priorRows = TargetRows.ToArray();
            _priorCompletion = IsCompleted;
            TargetRows.Clear();
            IsCompleted = false;
        }

        public Task RollbackAsync(CancellationToken _)
        {
            TargetRows.Clear();
            TargetRows.AddRange(_priorRows);
            IsCompleted = _priorCompletion;
            Rollbacks++;
            return Task.CompletedTask;
        }
    }
}
