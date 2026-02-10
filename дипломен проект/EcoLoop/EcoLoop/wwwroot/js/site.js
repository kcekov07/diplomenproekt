// EcoLoop global scripts

window.addEventListener('DOMContentLoaded', () => {
    const navToggle = document.getElementById('navToggle');
    const mobileNav = document.getElementById('mobileNav');
    if (navToggle && mobileNav) {
        navToggle.addEventListener('click', () => {
            const expanded = navToggle.getAttribute('aria-expanded') === 'true';
            navToggle.setAttribute('aria-expanded', (!expanded).toString());
            mobileNav.style.display = expanded ? 'none' : 'block';
            mobileNav.setAttribute('aria-hidden', expanded ? 'true' : 'false');
        });

        document.addEventListener('click', (event) => {
            if (!mobileNav.contains(event.target) && !navToggle.contains(event.target)) {
                mobileNav.style.display = 'none';
                mobileNav.setAttribute('aria-hidden', 'true');
                navToggle.setAttribute('aria-expanded', 'false');
            }
        });
    }
});