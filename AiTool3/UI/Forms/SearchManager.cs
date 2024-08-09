using AiTool3.Conversations;
using AiTool3.ExtensionMethods;

namespace AiTool3.UI.Forms
{
    public class SearchManager
    {
        private CancellationTokenSource? _cts;
        private readonly DataGridView _dgvConversations;

        public SearchManager(DataGridView dgvConversations)
        {
            _dgvConversations = dgvConversations;
        }

        public async Task PerformSearch(string searchText)
        {
            _cts = ResetCancellationToken(_cts);

            try
            {
                const int batchSize = 10;
                var rows = _dgvConversations.Rows.Cast<DataGridViewRow>().ToList();
                for (int i = 0; i < rows.Count; i += batchSize)
                {
                    var batch = rows.Skip(i).Take(batchSize);
                    var tasks = batch.Select(async row =>
                    {
                        var guid = row.Cells[0].Value?.ToString();
                        if (guid != null)
                        {
                            bool isVisible = await IsConversationVisible(guid, searchText, _cts.Token);
                            return (row, isVisible);
                        }
                        return (row, true);
                    }).ToList();

                    while (tasks.Any())
                    {
                        var completedTask = await Task.WhenAny(tasks);
                        tasks.Remove(completedTask);

                        var (row, isVisible) = await completedTask;
                        _dgvConversations.InvokeIfNeeded(() => row.Visible = isVisible);

                        _cts.Token.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, do nothing
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during search: {ex.Message}");
            }
        }

        private static CancellationTokenSource ResetCancellationToken(CancellationTokenSource? cts)
        {
            cts?.Cancel();
            return new CancellationTokenSource();
        }

        private static Task<bool> IsConversationVisible(string guid, string searchText, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var conv = BranchedConversation.LoadConversation(guid);
                return conv.Messages
                    .Where(m => m.Content != null)
                    .Any(m => m.Content!.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }, cancellationToken);
        }

        public void ClearSearch()
        {
            foreach (DataGridViewRow row in _dgvConversations.Rows)
            {
                row.Visible = true;
            }
        }
    }
}