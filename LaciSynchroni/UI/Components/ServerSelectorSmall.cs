using Dalamud.Bindings.ImGui;

namespace LaciSynchroni.UI.Components
{
    /// <summary>
    /// Quick and dirty server selector that can be used in all places where we need a server selector now, to be replaced with a better
    /// concept down the line
    /// </summary>
    /// <param name="onServerIndexChange">Event called when the server index selector in this selector has changed</param>
    public class ServerSelectorSmall(Action<int> onServerIndexChange, int currentServerIndex = 0)
    {
        private int _currentServerIndex = currentServerIndex;

        public void Draw(string[] availableServers, int[] connectedServers, float width)
        {
            if (connectedServers.Length <= 0)
            {
                // This component should not be rendered without any connected servers! Doesn't make much sense!
                return;
            }
            
            var selectedServer = availableServers[_currentServerIndex];
            ImGui.SetNextItemWidth(width);
            if (ImGui.BeginCombo("", selectedServer))
            {
                for (var i = 0; i < availableServers.Length; i++)
                {
                    var serverName = availableServers[i];
                    var isSelected = _currentServerIndex == i;
                    var isConnected = connectedServers.Contains(i);
                    if (ImGui.Selectable(serverName, isSelected))
                    {
                        ChangeSelectedIndex(i);
                    }
                    if (!isConnected)
                    {
                        UiSharedService.AttachToolTip($"You are currently not connected to {serverName} service.");
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private void ChangeSelectedIndex(int index)
        {
            if (_currentServerIndex != index)
            {
                _currentServerIndex = index;
                onServerIndexChange.Invoke(index);
            }
        }
    }
}