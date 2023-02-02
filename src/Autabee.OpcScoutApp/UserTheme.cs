namespace Autabee.OpcScoutApp
{
    public class UserTheme
    {
        bool dark;
        public bool Dark
        {
            get => dark; set
            {
                if (dark != value)
                {
                    dark = value;
                    ThemeChanged?.Invoke(this, null);
                }
            }
        }
        public string DarkString { get => Dark ? "dark" : "light"; }
        public string NavDarkString { get => navdark ? "dark" : "light"; }
        public string Nav { get; set; }
        string theme;
        public string Theme
        {
            get => theme; set
            {
                if (theme != value && value != null)
                {
                    theme = value;
                    ThemeChanged?.Invoke(this, null);
                }
            }
        }
        public string ThemeCss { get => $"{Theme}.{DarkString}.css"; }
        public string NavThemeCss { get => $"nav.{NavDarkString}.css"; }
        bool navdark;
        public bool NavDark { get => navdark; set => navdark = value; }
        public UserTheme()
        {
        }

        public event EventHandler ThemeChanged;
    }
}
