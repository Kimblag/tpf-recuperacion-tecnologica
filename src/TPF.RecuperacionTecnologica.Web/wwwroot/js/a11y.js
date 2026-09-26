(function () {
    var STORAGE_KEY = 'a11y';
    var DEFAULTS = { fs: '1', contrast: 'normal', motion: 'normal' };

    function load() {
        try {
            var saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
            return saved ? Object.assign({}, DEFAULTS, saved) : Object.assign({}, DEFAULTS);
        } catch (e) {
            return Object.assign({}, DEFAULTS);
        }
    }

    var state = load();
    if (!localStorage.getItem(STORAGE_KEY) && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        state.motion = 'reduce';
    }

    function save() {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(state)); } catch (e) { }
    }

    function apply() {
        var html = document.documentElement;
        html.style.setProperty('--fs', state.fs);
        html.setAttribute('data-contrast', state.contrast);
        html.setAttribute('data-motion', state.motion);
        syncControls();
    }

    function syncControls() {
        document.querySelectorAll('input[name="a11y-fs"]').forEach(function (el) {
            el.checked = el.value === state.fs;
        });
        document.querySelectorAll('input[name="a11y-contrast"]').forEach(function (el) {
            el.checked = el.value === state.contrast;
        });
        var motion = document.getElementById('a11y-motion');
        if (motion) motion.checked = state.motion === 'reduce';
    }

    function setTextScale(value) { state.fs = value; apply(); save(); }
    function setContrast(value) { state.contrast = value; apply(); save(); }
    function setMotion(value) { state.motion = value; apply(); save(); }
    function reset() { state = Object.assign({}, DEFAULTS); apply(); save(); }

    apply();

    window.RTA11y = {
        state: state,
        setTextScale: setTextScale,
        setContrast: setContrast,
        setMotion: setMotion,
        reset: reset
    };
})();