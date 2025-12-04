using System.Text.Json;

namespace Bimload.Gui.Services;

public class StateManager
{
    private readonly string _stateFilePath;

    public StateManager(string stateFilePath)
    {
        _stateFilePath = stateFilePath ?? throw new ArgumentNullException(nameof(stateFilePath));
    }

    public void SaveState(List<ProgramState> states)
    {
        try
        {
            var json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
        }
    }

    public List<ProgramState> LoadState()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                return new List<ProgramState>();
            }

            var json = File.ReadAllText(_stateFilePath);
            var states = JsonSerializer.Deserialize<List<ProgramState>>(json);
            return states ?? new List<ProgramState>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading state: {ex.Message}");
            return new List<ProgramState>();
        }
    }
}

public class ProgramState
{
    public string FileName { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = true;
}

