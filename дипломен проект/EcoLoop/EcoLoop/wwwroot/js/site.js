
javascript EcoLoop\wwwroot\js\site.js
// site.js - small helpers for navbar (mobile toggle) and accessibility
document.addEventListener('DOMContentLoaded', function () {
    var navToggle = document.getElementById('navToggle');
    var mobileNav = document.getElementById('mobileNav');

    if (navToggle && mobileNav) {
        navToggle.addEventListener('click', function () {
            var expanded = navToggle.getAttribute('aria-expanded') === 'true';
            navToggle.setAttribute('aria-expanded', (!expanded).toString());
            mobileNav.style.display = expanded ? 'none' : 'block';
            mobileNav.setAttribute('aria-hidden', expanded ? 'true' : 'false');
        });

        // close mobile nav when clicking outside
        document.addEventListener('click', function (e) {
            if (!mobileNav.contains(e.target) && !navToggle.contains(e.target)) {
                mobileNav.style.display = 'none';
                mobileNav.setAttribute('aria-hidden', 'true');
                navToggle.setAttribute('aria-expanded', 'false');
            }
        });
    }
});// Minimal JS for future enhancements (map, filters, etc.)
document.addEventListener('DOMContentLoaded', () => {
    // Placeholder: initialize map or UI hooks
    console.debug('EcoFind site ready');
});