function SetTheme(ThemeName) {

    var Theme = document.getElementById(`Theme`);
    Theme.href = `/css/` + ThemeName;
}