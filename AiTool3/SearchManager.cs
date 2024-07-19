using AiTool3.Conversations;
using AiTool3.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3
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
                foreach (DataGridViewRow row in _dgvConversations.Rows)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var guid = row.Cells[0].Value?.ToString();

                    if (guid != null)
                    {
                        bool isVisible = await IsConversationVisible(guid, searchText, _cts.Token);

                        _dgvConversations.InvokeIfNeeded(() =>
                        {
                            row.Visible = isVisible;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    MessageBox.Show($"An error occurred during search: {ex.Message}");
                }
            }
        }

        private static CancellationTokenSource ResetCancellationToken(CancellationTokenSource? cts)
        {
            cts?.Cancel();
            return new CancellationTokenSource();
        }

        private static async Task<bool> IsConversationVisible(string guid, string searchText, CancellationToken cancellationToken)
        {
            var conv = BranchedConversation.LoadConversation(guid);
            var allMessages = conv.Messages.Select(m => m.Content).ToList();

            foreach (string? message in allMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message != null && message!.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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