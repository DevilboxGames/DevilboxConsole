using Godot;

public partial class Caret : TextureRect
{
	[Export]
	public float FlickerTime { get; set; } = 0.2f;
	private double time = 0;

	private bool flickOff = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		time += delta;
		if (time >= FlickerTime)
		{
			flickOff = !flickOff;
			time = 0;
		}
		Modulate = Modulate with { A = flickOff ? 0.25f : 1.0f };
	}
}
