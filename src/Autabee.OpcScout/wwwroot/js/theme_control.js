function SetTheme(ThemeName) {

    var Theme = document.getElementById(`Theme`);
    Theme.href = `_content/Autabee.OpcScout.BlazorView/css/` + ThemeName;
}

function SetNavTheme(ThemeName) {

    var Theme = document.getElementById(`NavTheme`);
    Theme.href = `_content/Autabee.OpcScout.BlazorView/css/` + ThemeName;
}