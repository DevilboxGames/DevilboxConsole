using Godot;

public partial class ConsoleInput : Label
{
	[Export]
	public int CaretPosition { get; set; }
	[Export]
	public TextureRect CaretTexture { get; set; }
	private TextParagraph _paragraph;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_paragraph = new TextParagraph();
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_paragraph.Clear();
		if (CaretPosition > 0)
		{
			_paragraph.AddString(Text.Substring(0, CaretPosition), GetThemeFont("font"), GetThemeFontSize("font_size"));
		}

		_paragraph.AddObject("Caret", Vector2.Zero);
			
		if (CaretPosition < Text.Length)
		{
			_paragraph.AddString(Text.Substring(CaretPosition), GetThemeFont("font"), GetThemeFontSize("font_size"));
		}

		if (Text.Length == 0)
		{
			
			_paragraph.AddString("", GetThemeFont("font"), GetThemeFontSize("font_size"));
		}

		for (int i = 0; i < _paragraph.GetLineCount(); i++)
		{
			foreach (var lineObject in _paragraph.GetLineObjects(i))
			{
				var objName = lineObject.AsString();
				if (objName == "Caret")
				{
					var rect = _paragraph.GetLineObjectRect(i, lineObject);
					
					CaretTexture.Position = rect.Position with {Y=0};
				}
			}
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		_paragraph.Draw(GetCanvasItem(), Vector2.Zero);
	}
}
