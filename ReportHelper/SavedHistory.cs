using ReportHelper.Core;

namespace ReportHelper;

internal sealed class SavedHistory
{
    private HistoryFile? file;

    public SavedHistory(string path)
    {
        this.file = new HistoryFile(path);
        this.Session = new Session(this.Load());
    }

    public Session Session { get; }

    public void Save(DateTime now)
    {
        var pruned = this.Session.Prune(now);
        var unsaved = this.Session.TakeUnsaved();
        this.Write(file =>
        {
            if (pruned)
            {
                file.Rewrite(this.Session.Entries);
            }
            else
            {
                file.Append(unsaved);
            }
        });
    }

    public void Clear()
    {
        this.Session.Clear();
        this.Write(file => file.Rewrite([]));
    }

    private static bool IsFileFailure(Exception e) => e is IOException or UnauthorizedAccessException;

    private IReadOnlyList<Entry> Load()
    {
        try
        {
            return this.file!.Load();
        }
        catch (Exception e) when (IsFileFailure(e))
        {
            Services.Log.Error(e, "ReportHelper: saved history cannot be read, starting empty");
            return [];
        }
    }

    private void Write(Action<HistoryFile> write)
    {
        if (this.file == null)
        {
            return;
        }

        try
        {
            write(this.file);
        }
        catch (Exception e) when (IsFileFailure(e))
        {
            Services.Log.Error(e, "ReportHelper: history cannot be saved, it is kept in memory only until the next start");
            this.file = null;
        }
    }
}
