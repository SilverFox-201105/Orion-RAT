using Orion.Server.Controls.Wpf;
using Orion.Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

#nullable enable

namespace Orion.Server.Controls
{
    public sealed class ClientsListElementHost : ElementHost
    {
        private readonly ClientsListView _clientsListView;

        public ClientsListElementHost()
        {
            _clientsListView = new ClientsListView();
            Child = _clientsListView;
            Dock = DockStyle.Fill;

            _clientsListView.SelectionChanged += ClientsListViewOnSelectionChanged;
            _clientsListView.ItemDoubleClicked += ClientsListViewOnItemDoubleClicked;
            _clientsListView.FavoriteToggled += ClientsListViewOnFavoriteToggled;
        }

        public event EventHandler? SelectionChanged;
        public event EventHandler<Client>? ItemDoubleClicked;
        public event EventHandler<Client>? FavoriteToggled;

        public IReadOnlyList<Client> SelectedClients => _clientsListView.SelectedEntries.Select(e => e.Client).ToList();

        public int SelectedCount => SelectedClients.Count;

        public ClientListEntry AddOrUpdate(Client client, Action<ClientListEntry> updater)
        {
            return _clientsListView.AddOrUpdate(client, updater);
        }

        public void Remove(Client client) => _clientsListView.Remove(client);

        public void ClearClients() => _clientsListView.Clear();

        public void ApplyFilter(Func<ClientListEntry, bool>? predicate)
        {
            _clientsListView.ApplyFilter(predicate == null ? null : new Predicate<ClientListEntry>(predicate));
        }

        public void SetGrouping(ClientGroupMode mode) => _clientsListView.SetGrouping(mode);

        public void RefreshSort() => _clientsListView.RefreshSort();

        public void SetSelectedClients(IEnumerable<Client> clients)
        {
            _clientsListView.SetSelectedClients(clients);
        }

        public void RefreshItem(Client client)
        {
            var entry = _clientsListView.GetEntryByClient(client);
            if (entry != null)
            {
                _clientsListView.RefreshItem(entry);
            }
        }

        public void ApplyTheme(bool isDarkMode) => _clientsListView.ApplyTheme(isDarkMode);

        public void SetBackgroundImage(System.Drawing.Image? image, float opacity = 0.3f)
        {
            _clientsListView.SetBackgroundImage(image, opacity);
        }

        public void SetToolTip(Client client, string text)
        {
            var entry = _clientsListView.GetEntryByClient(client);
            if (entry != null)
            {
                entry.ToolTip = text;
            }
        }

        public ClientListEntry? GetEntry(Client client) => _clientsListView.GetEntryByClient(client);

        private void ClientsListViewOnSelectionChanged(object? sender, IReadOnlyList<ClientListEntry> e)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClientsListViewOnItemDoubleClicked(object? sender, ClientListEntry e)
        {
            ItemDoubleClicked?.Invoke(this, e.Client);
        }

        private void ClientsListViewOnFavoriteToggled(object? sender, ClientListEntry e)
        {
            FavoriteToggled?.Invoke(this, e.Client);
        }
    }
}
