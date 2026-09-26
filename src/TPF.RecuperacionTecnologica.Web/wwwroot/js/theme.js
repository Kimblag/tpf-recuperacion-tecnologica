(function () {
    var STORAGE_KEY = 'theme';

    function getStoredTheme() {
        try { return localStorage.getItem(STORAGE_KEY); } catch (e) { return null; }
    }

    function setStoredTheme(theme) {
        try { localStorage.setItem(STORAGE_KEY, theme); } catch (e) { }
    }

    function getPreferredTheme() {
        return getStoredTheme() || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }

    function toggleTheme() {
        var next = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        setStoredTheme(next);
        applyTheme(next);
    }

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () {
        if (!getStoredTheme()) applyTheme(getPreferredTheme());
    });

    window.RTTheme = { toggle: toggleTheme, apply: applyTheme, preferred: getPreferredTheme };
})();