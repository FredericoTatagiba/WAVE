namespace WAVE.Application.Abstractions;

/// <summary>
/// Opens the terminal window with a continuous ping visible to the technician
/// (terminal persistence required by the specification).
/// </summary>
public interface IVisiblePingTerminal
{
    void Launch(string host);

    void Close();
}
