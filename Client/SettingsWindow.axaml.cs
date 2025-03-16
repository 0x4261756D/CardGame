using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CardGameUtils;

namespace CardGameClient;

internal partial class SettingsWindow : Window
{
	public SettingsWindow()
	{
		InitializeComponent();
		WidthInput.Value = Program.config.width;
		HeightInput.Value = Program.config.height;
		ShouldSpawnCoreInput.IsChecked = Program.config.should_spawn_core;
		ShouldSavePlayerNameInput.IsChecked = Program.config.should_save_player_name;
		AnimationDelayInput.Value = Program.config.animation_delay_in_ms;
		CoreArgsInput.Text = Program.config.core_info.Arguments;
		foreach(ClientConfig.ThemeVariant themeVariant in Enum.GetValues<ClientConfig.ThemeVariant>())
		{
			ThemeInput.Items.Add(themeVariant);
		}
		ThemeInput.SelectedItem = Program.config.theme;
		if(ThemeInput.SelectedIndex < 0)
		{
			ThemeInput.SelectedIndex = 0;
		}
	}
	public void BackClick(object sender, RoutedEventArgs args)
	{
		new MainWindow
		{
			WindowState = WindowState,
		}.Show();
		if(Application.Current is not null)
		{
			Application.Current.RequestedThemeVariant = UIUtils.ConvertThemeVariant((ClientConfig.ThemeVariant?)ThemeInput.SelectedItem);
		}
		Program.config.width = (int?)WidthInput.Value ?? 1080;
		Program.config.height = (int?)HeightInput.Value ?? 720;
		Program.config.should_spawn_core = ShouldSpawnCoreInput.IsChecked ?? false;
		Program.config.should_save_player_name = ShouldSavePlayerNameInput.IsChecked ?? false;
		Program.config.animation_delay_in_ms = (int?)AnimationDelayInput.Value ?? 120;
		Program.config.core_info.Arguments = CoreArgsInput.Text ?? "--mode=client --config=../../../config/config.json --additional_cards_url=h2871632.stratoserver.net";
		Program.config.theme = (ClientConfig.ThemeVariant?)ThemeInput.SelectedItem;
		Close();
	}
	private void ChangeThemeVariant(object? sender, SelectionChangedEventArgs args)
	{
		if(Application.Current != null)
		{
			Application.Current.RequestedThemeVariant = UIUtils.ConvertThemeVariant((ClientConfig.ThemeVariant?)ThemeInput.SelectedItem);
		}
	}
}
