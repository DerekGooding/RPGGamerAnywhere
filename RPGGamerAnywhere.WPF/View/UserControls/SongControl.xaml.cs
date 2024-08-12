using RPGGamerAnywhere.Model.Database;
using System.Windows.Controls;

namespace RPGGamerAnywhere.WPF.View.UserControls;

public partial class SongControl : UserControl
{
    public Song Song {
        get { return (Song)GetValue(SongProperty); }
        set { SetValue(SongProperty, value); }
    }
    public static readonly DependencyProperty SongProperty =
        DependencyProperty.Register("Song", typeof(Song),
            typeof(SongControl), new PropertyMetadata(null, SetValues));
    private static void SetValues(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SongControl songControl)
            songControl.DataContext = songControl.Song;
    }
    public SongControl() => InitializeComponent();
}
