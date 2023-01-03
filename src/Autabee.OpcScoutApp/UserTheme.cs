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
                    ThemeChanged?.Invoke(this,null);
                }
            }
        }
        public string DarkString { get => Dark ? "dark" : "light"; }
        string theme;
        public string Theme { get => theme; set
            {
                if (theme != value && value != null)
                {
                    theme = value;
                    ThemeChanged?.Invoke(this, null);
                }
            }
        }
        public string ThemeCss { get => $"{Theme}.{DarkString}.css"; }
        public UserTheme()
        {
        }

        public event EventHandler ThemeChanged;
    }
}
