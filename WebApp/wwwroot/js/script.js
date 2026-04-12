// ========================== Theming ===========================

// Get theme button and icon
const themeToggle = document.getElementById("theme-toggle");
const themeIcon = themeToggle ? themeToggle.querySelector("span") : null;

// Track system theme preference and update it on change
const sysThemeQuery = window.matchMedia("(prefers-color-scheme: dark)");
let sysTheme = sysThemeQuery.matches ? "dark" : "light";
sysThemeQuery.addEventListener(
    "change", e => sysTheme = e.matches ? "dark" : "light"
);

// Apply stored theme preference or default to system theme
const storedTheme = localStorage.getItem("theme");
let theme = storedTheme ? storedTheme : sysTheme;
applyTheme(theme);


// Toggle theme on link click and store preference
if (themeToggle) {
    themeToggle.addEventListener("click", () => {
        theme = (theme === "light") ? "dark" : "light";
        applyTheme(theme);
        localStorage.setItem("theme", theme);
    });
}

function applyTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    document.documentElement.setAttribute("data-bs-theme", theme);
    if (themeIcon) {
        themeIcon.className = (theme === "light") ? "fa-solid fa-moon" : "fa-solid fa-sun";
    }
}