using Godot;

namespace DevilboxConsole.examples.Shared.scripts.interfaces;

public interface IInteractableObject
{
    bool IsInteractable();
    void Interact(Node instigator);
    void Cancel(Node instigator);
}