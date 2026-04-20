document.addEventListener('DOMContentLoaded', function () {
    var navToggle = document.getElementById('navToggle');
    var mobileNav = document.getElementById('mobileNav');
    var desktopBreakpoint = window.matchMedia('(min-width: 1081px)');

    if (!navToggle || !mobileNav) {
        return;
    }

    function closeMobileNav() {
        mobileNav.style.display = 'none';
        mobileNav.setAttribute('aria-hidden', 'true');
        navToggle.setAttribute('aria-expanded', 'false');
    }

    navToggle.addEventListener('click', function () {
        var expanded = navToggle.getAttribute('aria-expanded') === 'true';
        navToggle.setAttribute('aria-expanded', (!expanded).toString());
        mobileNav.style.display = expanded ? 'none' : 'block';
        mobileNav.setAttribute('aria-hidden', expanded ? 'true' : 'false');
    });

    document.addEventListener('click', function (e) {
        if (!mobileNav.contains(e.target) && !navToggle.contains(e.target)) {
            closeMobileNav();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeMobileNav();
        }
    });

    desktopBreakpoint.addEventListener('change', function (event) {
        if (event.matches) {
            closeMobileNav();
        }
    });
}); 