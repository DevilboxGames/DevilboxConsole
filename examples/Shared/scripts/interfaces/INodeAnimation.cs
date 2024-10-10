namespace DevilboxConsole.examples.Shared.scripts.interfaces;

public interface INodeAnimation
{
    bool IsPlaying { get; protected set; }
    bool AutoPlay { get;  set; }
    void Play();
    void Stop();
    void Restart();
}