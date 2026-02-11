// site.js - helpers for navbar (mobile toggle)
document.addEventListener('DOMContentLoaded', function () {
    var navToggle = document.getElementById('navToggle');
    var mobileNav = document.getElementById('mobileNav');

    if (!navToggle || !mobileNav) {
        return;
    }
    navToggle.addEventListener('click', function () {
        var expanded = navToggle.getAttribute('aria-expanded') === 'true';
        navToggle.setAttribute('aria-expanded', (!expanded).toString());
        mobileNav.style.display = expanded ? 'none' : 'block';
        mobileNav.setAttribute('aria-hidden', expanded ? 'true' : 'false');
    });

    document.addEventListener('click', function (e) {
        if (!mobileNav.contains(e.target) && !navToggle.contains(e.target)) {
            mobileNav.style.display = 'none';
            mobileNav.setAttribute('aria-hidden', 'true');
            navToggle.setAttribute('aria-expanded', 'false');
        }
    });
});