using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CardGameClient;

public partial class ErrorPopup : Window
{
	public ErrorPopup(string msg)
	{
		InitializeComponent();
		MessageBlock.Text = msg;
		Width = Program.config.width / 2;
		Height = Program.config.height / 2;
		Topmost = true;
	}

	private void CloseClick(object? sender, RoutedEventArgs args)
	{
		Close();
	}
}
